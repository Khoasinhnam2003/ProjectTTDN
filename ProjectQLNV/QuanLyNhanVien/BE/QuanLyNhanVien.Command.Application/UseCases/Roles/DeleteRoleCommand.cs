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
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Roles
{
    public record DeleteRoleCommand : IRequest<Result<bool>>
    {
        public int RoleId { get; set; }
    }

    public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteRoleCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("RoleId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var role = await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
                    return role != null;
                }).WithMessage("Vai trò với RoleId đã cho không tồn tại.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var userRole = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id, cancellationToken);
                    return !userRole;
                }).WithMessage("Không thể xóa vai trò vì đã có người dùng liên kết với vai trò này.");
        }
    }

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeleteRoleCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DeleteRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteRoleCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting deletion of role with ID: {RoleId}", request.RoleId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for role ID {RoleId}: {Errors}", request.RoleId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
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
                    return Result<bool>.Failure(new Error("Vai trò không tồn tại."));
                }

                roleRepository.Delete(role);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully deleted role with ID: {RoleId}", request.RoleId);
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when deleting role with ID: {RoleId}", request.RoleId);
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa vai trò."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
                {
                    _logger.LogWarning("Cannot delete role with ID {RoleId} due to foreign key constraint", request.RoleId);
                    return Result<bool>.Failure(new Error("Không thể xóa vai trò vì đã có người dùng liên kết với vai trò này."));
                }
                _logger.LogError(ex, "Error deleting role with ID: {RoleId}", request.RoleId);
                return Result<bool>.Failure(new Error($"Lỗi khi xóa vai trò: {ex.Message}"));
            }
        }
    }
}
