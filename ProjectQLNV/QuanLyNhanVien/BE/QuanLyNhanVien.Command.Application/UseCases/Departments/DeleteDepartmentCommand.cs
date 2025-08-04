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
    public record DeleteDepartmentCommand : IRequest<Result<bool>>
    {
        public int DepartmentId { get; set; }
    }

    public class DeleteDepartmentCommandValidator : AbstractValidator<DeleteDepartmentCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteDepartmentCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("ID phòng ban không được để trống.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var department = await _context.Departments.FindAsync(new object[] { id }, cancellationToken);
                    return department != null;
                }).WithMessage("Phòng ban với ID đã cho không tồn tại.");
        }
    }

    public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeleteDepartmentCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteDepartmentCommandHandler> _logger;

        public DeleteDepartmentCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DeleteDepartmentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteDepartmentCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting deletion of department with ID: {DepartmentId}", request.DepartmentId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for department ID {DepartmentId}: {Errors}", request.DepartmentId, errorMessages);
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
                    return Result<bool>.Failure(new Error("Phòng ban không tồn tại."));
                }

                departmentRepository.Delete(department);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully deleted department with ID: {DepartmentId}", request.DepartmentId);
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when deleting department with ID: {DepartmentId}", request.DepartmentId);
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa phòng ban."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
                {
                    _logger.LogWarning("Cannot delete department with ID {DepartmentId} due to foreign key constraint", request.DepartmentId);
                    return Result<bool>.Failure(new Error("Không thể xóa phòng ban vì có nhân viên đang thuộc phòng ban này."));
                }
                _logger.LogError(ex, "Error deleting department with ID: {DepartmentId}", request.DepartmentId);
                return Result<bool>.Failure(new Error($"Lỗi khi xóa phòng ban: {ex.Message}"));
            }
        }
    }
}
