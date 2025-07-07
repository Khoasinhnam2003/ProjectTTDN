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

namespace QuanLyNhanVien.Command.Application.UseCases.Jwt
{
    public record CreateUserCommand : IRequest<Result<User>>
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>();
    }

    // Validator cho CreateUserCommand
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateUserCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0)
                .WithMessage("EmployeeId phải lớn hơn 0.")
                .CustomAsync(async (employeeId, context, cancellationToken) =>
                {
                    var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken);
                    if (employee == null)
                    {
                        context.AddFailure("EmployeeId không tồn tại.");
                    }
                });

            RuleFor(x => x.Username)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Tên đăng nhập không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[a-zA-Z0-9_]+$"))
                .WithMessage("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới.")
                .CustomAsync(async (username, context, cancellationToken) =>
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
                    if (user != null)
                    {
                        context.AddFailure("Tên đăng nhập đã được sử dụng.");
                    }
                });

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
                .Matches(new Regex("^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d]{8,}$"))
                .WithMessage("Mật khẩu phải chứa ít nhất một chữ cái và một số.");

            // Xác thực RoleIds
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

    // Handler để xử lý CreateUserCommand
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<User>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateUserCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public CreateUserCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateUserCommandValidator(context);
        }

        public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // Thực hiện validation
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<User>.Failure(new Error(errorMessages));
            }

            // Mã hóa mật khẩu sử dụng BCrypt
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);

            var user = new User
            {
                EmployeeId = request.EmployeeId,
                Username = request.Username,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Thêm user
                var userRepository = _unitOfWork.Repository<User, int>();
                userRepository.Add(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Thêm vai trò vào UserRole nếu có
                if (request.RoleIds != null && request.RoleIds.Any())
                {
                    var userRoleRepository = _unitOfWork.Repository<UserRole, (int, int)>();
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
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                transaction.Commit();

                // Lấy lại user với thông tin đầy đủ
                var createdUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId, cancellationToken);

                if (createdUser == null)
                {
                    return Result<User>.Failure(new Error("Không thể tìm thấy tài khoản vừa tạo."));
                }

                return Result<User>.Success(createdUser);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<User>.Failure(new Error($"Lỗi khi tạo tài khoản: {ex.Message}"));
            }
        }
    }
}
