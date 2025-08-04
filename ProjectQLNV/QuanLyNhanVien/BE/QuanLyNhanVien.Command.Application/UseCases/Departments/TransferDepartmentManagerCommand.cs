using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Domain.Entities;
using QuanLyNhanVien.Command.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Departments
{
    public record TransferDepartmentManagerCommand : IRequest<Result<bool>>
    {
        public int DepartmentId { get; set; }
        public int NewManagerId { get; set; }
    }

    public class TransferDepartmentManagerCommandValidator : AbstractValidator<TransferDepartmentManagerCommand>
    {
        private readonly ApplicationDbContext _context;

        public TransferDepartmentManagerCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("DepartmentId must be greater than 0.");
            RuleFor(x => x.NewManagerId)
                .GreaterThan(0).WithMessage("NewManagerId must be greater than 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
                    return employee != null;
                }).WithMessage("New manager does not exist.");
        }
    }

    public class TransferDepartmentManagerCommandHandler : IRequestHandler<TransferDepartmentManagerCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly TransferDepartmentManagerCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransferDepartmentManagerCommandHandler> _logger;

        public TransferDepartmentManagerCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<TransferDepartmentManagerCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new TransferDepartmentManagerCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(TransferDepartmentManagerCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting transfer of manager for department ID: {DepartmentId} to new manager ID: {NewManagerId}", request.DepartmentId, request.NewManagerId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for department ID {DepartmentId} and new manager ID {NewManagerId}: {Errors}", request.DepartmentId, request.NewManagerId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var departmentRepository = _unitOfWork.Repository<Department, int>();
                var department = await departmentRepository.FindAsync(request.DepartmentId, cancellationToken);
                if (department == null)
                {
                    transaction.Rollback();
                    _logger.LogWarning("Department with ID {DepartmentId} not found", request.DepartmentId);
                    return Result<bool>.Failure(new Error("Department not found."));
                }

                department.ManagerId = request.NewManagerId;
                department.UpdatedAt = DateTime.Now; // 01:33 PM +07, 30/07/2025 (based on current date/time)
                departmentRepository.Update(department);

                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully transferred manager for department ID: {DepartmentId} to new manager ID: {NewManagerId}", request.DepartmentId, request.NewManagerId);
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when transferring manager for department ID: {DepartmentId}", request.DepartmentId);
                return Result<bool>.Failure(new Error("No changes were made when transferring department manager."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error transferring manager for department ID: {DepartmentId}", request.DepartmentId);
                return Result<bool>.Failure(new Error($"Error transferring department manager: {ex.Message}"));
            }
        }
    }
}
