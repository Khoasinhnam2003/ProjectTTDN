using FluentValidation;
using MediatR;
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

        public TransferDepartmentManagerCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new TransferDepartmentManagerCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(TransferDepartmentManagerCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
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
                    return Result<bool>.Failure(new Error("Department not found."));
                }

                department.ManagerId = request.NewManagerId;
                department.UpdatedAt = DateTime.Now; // 03:30 PM +07, 01/07/2025
                departmentRepository.Update(department);

                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    Console.WriteLine("Thành công");
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                return Result<bool>.Failure(new Error("No changes were made when transferring department manager."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<bool>.Failure(new Error($"Error transferring department manager: {ex.Message}"));
            }
        }
    }
}
