using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<UpdateRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateRoleCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Role>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting update for role with ID: {RoleId}", request.RoleId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for role ID {RoleId}: {Errors}", request.RoleId, errorMessages);
                return Result<Role>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var roleRepository = _unitOfWork.Repository<Role, int>();
                var role = await roleRepository.FindAsync(request.RoleId, cancellationToken);
                if (role == null)
                {
                    transaction.Rollback();
                    _logger.LogWarning("Role with ID {RoleId} not found", request.RoleId);
                    return Result<Role>.Failure(new Error("Vai trò không tồn tại."));
                }

                role.RoleName = request.RoleName;
                role.Description = request.Description;
                role.UpdatedAt = DateTime.Now; // 01:43 PM +07, 30/07/2025

                roleRepository.Update(role);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully updated role with ID: {RoleId}", request.RoleId);
                    return Result<Role>.Success(role);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when updating role with ID: {RoleId}", request.RoleId);
                return Result<Role>.Failure(new Error("Không có thay đổi nào được thực hiện khi cập nhật vai trò."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error updating role with ID: {RoleId}", request.RoleId);
                return Result<Role>.Failure(new Error($"Lỗi khi cập nhật vai trò: {ex.Message}"));
            }
        }
    }
}
