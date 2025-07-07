using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Domain.Entities;
using QuanLyNhanVien.Command.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Departments
{
    public record UpdateDepartmentCommand : IRequest<Result<bool>>
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Location { get; set; }
        public int? ManagerId { get; set; }
    }

    public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateDepartmentCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("ID phòng ban phải lớn hơn 0.");

            RuleFor(x => x.DepartmentName)
                .NotEmpty().WithMessage("Tên phòng ban không được để trống.")
                .MaximumLength(100).WithMessage("Tên phòng ban tối đa 100 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Tên phòng ban chỉ được chứa chữ cái và khoảng trắng.");

            RuleFor(x => x.Location)
                .MaximumLength(100).WithMessage("Địa điểm tối đa 100 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s0-9,.-]+$")).When(x => !string.IsNullOrEmpty(x.Location))
                .WithMessage("Địa điểm chỉ được chứa chữ cái, số, khoảng trắng, dấu phẩy, dấu chấm và dấu gạch ngang.");

            RuleFor(x => x.DepartmentName)
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    var command = (UpdateDepartmentCommand)context.InstanceToValidate;
                    var duplicate = await _context.Departments
                        .AnyAsync(d => d.DepartmentName == name && d.DepartmentId != command.DepartmentId, cancellationToken);
                    if (duplicate)
                    {
                        context.AddFailure("Tên phòng ban đã tồn tại.");
                    }
                });

            RuleFor(x => x.ManagerId)
                .CustomAsync(async (managerId, context, cancellationToken) =>
                {
                    if (managerId.HasValue)
                    {
                        var employee = await _context.Employees
                            .FindAsync(new object[] { managerId.Value }, cancellationToken);
                        if (employee == null)
                        {
                            context.AddFailure("Nhân viên được chỉ định làm quản lý không tồn tại.");
                        }
                    }
                });
        }
    }

    public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateDepartmentCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public UpdateDepartmentCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateDepartmentCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
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

                department.DepartmentName = request.DepartmentName;
                department.Location = request.Location;
                department.ManagerId = request.ManagerId;
                department.UpdatedAt = DateTime.Now;

                departmentRepository.Update(department);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    Console.WriteLine("Thành công");
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi cập nhật phòng ban."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<bool>.Failure(new Error($"Lỗi khi cập nhật phòng ban: {ex.Message}"));
            }
        }
    }
}
