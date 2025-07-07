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

        public DeleteDepartmentCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteDepartmentCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
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
                    return Result<bool>.Failure(new Error("Phòng ban không tồn tại."));
                }

                departmentRepository.Delete(department);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    Console.WriteLine("Thành công");
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa phòng ban."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
                {
                    return Result<bool>.Failure(new Error("Không thể xóa phòng ban vì có nhân viên đang thuộc phòng ban này."));
                }
                return Result<bool>.Failure(new Error($"Lỗi khi xóa phòng ban: {ex.Message}"));
            }
        }
    }
}
