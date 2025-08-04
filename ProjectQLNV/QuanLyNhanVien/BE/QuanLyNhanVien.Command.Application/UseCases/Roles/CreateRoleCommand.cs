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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Roles
{
    public record CreateRoleCommand : IRequest<Result<Role>>
    {
        public string RoleName { get; set; }
        public string Description { get; set; }
    }

    public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateRoleCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.RoleName)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Tên vai trò không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[a-zA-Z0-9_]+$"))
                .WithMessage("Tên vai trò chỉ được chứa chữ cái, số và dấu gạch dưới.")
                .CustomAsync(async (roleName, context, cancellationToken) =>
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);
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

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Role>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateRoleCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<CreateRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateRoleCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Role>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting creation of role with name: {RoleName}", request.RoleName);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for role name {RoleName}: {Errors}", request.RoleName, errorMessages);
                return Result<Role>.Failure(new Error(errorMessages));
            }

            var role = new Role
            {
                RoleName = request.RoleName,
                Description = request.Description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var roleRepository = _unitOfWork.Repository<Role, int>();
                roleRepository.Add(role);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully created role with name: {RoleName}, ID: {RoleId}", role.RoleName, role.RoleId);
                    return Result<Role>.Success(role);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when creating role with name: {RoleName}", request.RoleName);
                return Result<Role>.Failure(new Error("Không có thay đổi nào được thực hiện khi tạo vai trò."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error creating role with name: {RoleName}", request.RoleName);
                return Result<Role>.Failure(new Error($"Lỗi khi tạo vai trò: {ex.Message}"));
            }
        }
    }
}
