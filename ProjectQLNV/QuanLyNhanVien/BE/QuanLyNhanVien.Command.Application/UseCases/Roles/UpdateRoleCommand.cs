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

namespace QuanLyNhanVien.Command.Application.UseCases.Roles
{
    public record UpdateRoleCommand : IRequest<Result<Role>>
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
    }

    public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateRoleCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("RoleId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var role = await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
                    return role != null;
                }).WithMessage("Vai trò với RoleId đã cho không tồn tại.");

            RuleFor(x => x.RoleName)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Tên vai trò không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[a-zA-Z0-9_]+$"))
                .WithMessage("Tên vai trò chỉ được chứa chữ cái, số và dấu gạch dưới.")
                .CustomAsync(async (roleName, context, cancellationToken) =>
                {
                    var command = (UpdateRoleCommand)context.InstanceToValidate;
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName && r.RoleId != command.RoleId, cancellationToken);
                    if (role != null)
                    {
                        context.AddFailure("Tên vai trò đã tồn tại.");
                    }
                });

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("Mô tả tối đa 200 ký tự.");
        }
    }

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<Role>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateRoleCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public UpdateRoleCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateRoleCommandValidator(context);
        }

        public async Task<Result<Role>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<Role>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var roleRepository = _unitOfWork.Repository<Role, int>();
                var role = await roleRepository.FindAsync(request.RoleId, cancellationToken);
                if (role == null)
                {
                    return Result<Role>.Failure(new Error("Vai trò không tồn tại."));
                }

                role.RoleName = request.RoleName;
                role.Description = request.Description;
                role.UpdatedAt = DateTime.Now;

                roleRepository.Update(role);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                transaction.Commit();
                return Result<Role>.Success(role);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<Role>.Failure(new Error($"Lỗi khi cập nhật vai trò: {ex.Message}"));
            }
        }
    }
}
