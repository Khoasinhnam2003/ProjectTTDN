﻿using FluentValidation;
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

namespace QuanLyNhanVien.Command.Application.UseCases.Users
{
    public record UpdateUserCommand : IRequest<Result<User>>
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>();
    }

    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateUserCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("UserId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
                    return user != null;
                }).WithMessage("Tài khoản với UserId đã cho không tồn tại.");

            RuleFor(x => x.Username)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Tên đăng nhập không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[a-zA-Z0-9_]+$"))
                .WithMessage("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới.")
                .CustomAsync(async (username, context, cancellationToken) =>
                {
                    var command = (UpdateUserCommand)context.InstanceToValidate;
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.UserId != command.UserId, cancellationToken);
                    if (user != null)
                    {
                        context.AddFailure("Tên đăng nhập đã được sử dụng.");
                    }
                });

            RuleFor(x => x.Password)
                .NotEmpty().When(x => x.Password != null)
                .MinimumLength(8).When(x => x.Password != null)
                .WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
                .Matches(new Regex("^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d]{8,}$")).When(x => x.Password != null)
                .WithMessage("Mật khẩu phải chứa ít nhất một chữ cái và một số.");

            RuleFor(x => x.RoleIds)
                .NotNull()
                .WithMessage("Danh sách vai trò không được null.")
                .CustomAsync(async (roleIds, context, cancellationToken) =>
                {
                    if (roleIds != null && roleIds.Any())
                    {
                        var validRoleIds = await _context.Roles
                            .Where(r => roleIds.Contains(r.RoleId))
                            .Select(r => r.RoleId)
                            .ToListAsync(cancellationToken);

                        if (validRoleIds.Count != roleIds.Count)
                        {
                            context.AddFailure("Một hoặc nhiều RoleId không tồn tại.");
                        }
                    }
                });
        }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<User>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateUserCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public UpdateUserCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateUserCommandValidator(context);
        }

        public async Task<Result<User>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<User>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var userRepository = _unitOfWork.Repository<User, int>();
                var user = await userRepository.FindAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    return Result<User>.Failure(new Error("Tài khoản không tồn tại."));
                }

                user.Username = request.Username;
                if (!string.IsNullOrEmpty(request.Password))
                {
                    var salt = BCrypt.Net.BCrypt.GenerateSalt();
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);
                    user.PasswordSalt = salt;
                }
                user.UpdatedAt = DateTime.Now;

                userRepository.Update(user);

                // Cập nhật UserRole
                var userRoleRepository = _unitOfWork.Repository<UserRole, (int, int)>();
                var existingRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == request.UserId)
                    .ToListAsync(cancellationToken);
                foreach (var role in existingRoles)
                {
                    userRoleRepository.Delete(role);
                }

                if (request.RoleIds != null && request.RoleIds.Any())
                {
                    foreach (var roleId in request.RoleIds)
                    {
                        var userRole = new UserRole
                        {
                            UserId = user.UserId,
                            RoleId = roleId,
                            CreatedAt = DateTime.Now
                        };
                        userRoleRepository.Add(userRole);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                transaction.Commit();

                var updatedUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId, cancellationToken);

                if (updatedUser == null)
                {
                    return Result<User>.Failure(new Error("Không thể tìm thấy tài khoản vừa cập nhật."));
                }

                return Result<User>.Success(updatedUser);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<User>.Failure(new Error($"Lỗi khi cập nhật tài khoản: {ex.Message}"));
            }
        }
    }
}
