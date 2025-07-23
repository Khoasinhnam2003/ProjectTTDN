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
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Users
{
    public record DeleteUserCommand : IRequest<Result<bool>>
    {
        public int UserId { get; set; }
    }

    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteUserCommandValidator(ApplicationDbContext context)
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
        }
    }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeleteUserCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public DeleteUserCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteUserCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
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
                var userRepository = _unitOfWork.Repository<User, int>();
                var user = await userRepository.FindAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    return Result<bool>.Failure(new Error("Tài khoản không tồn tại."));
                }

                // Xóa các bản ghi UserRole liên quan
                var userRoleRepository = _unitOfWork.Repository<UserRole, (int, int)>();
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == request.UserId)
                    .ToListAsync(cancellationToken);
                foreach (var userRole in userRoles)
                {
                    userRoleRepository.Delete(userRole);
                }

                // Xóa các bản ghi UserToken liên quan
                var userTokenRepository = _unitOfWork.Repository<UserToken, int>();
                var userTokens = await _context.UserTokens
                    .Where(ut => ut.UserId == request.UserId)
                    .ToListAsync(cancellationToken);
                foreach (var userToken in userTokens)
                {
                    userTokenRepository.Delete(userToken);
                }

                // Xóa user
                userRepository.Delete(user);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa tài khoản."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<bool>.Failure(new Error($"Lỗi khi xóa tài khoản: {ex.Message}"));
            }
        }
    }
}
