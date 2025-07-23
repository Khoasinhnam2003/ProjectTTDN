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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Roles
{
    public record CreateRoleCommand : IRequest<Result<Role>>
    {
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        [StringLength(200)]
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

        public CreateRoleCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateRoleCommandValidator(context);
        }

        public async Task<Result<Role>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
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
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                transaction.Commit();
                return Result<Role>.Success(role);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<Role>.Failure(new Error($"Lỗi khi tạo vai trò: {ex.Message}"));
            }
        }
    }
}
