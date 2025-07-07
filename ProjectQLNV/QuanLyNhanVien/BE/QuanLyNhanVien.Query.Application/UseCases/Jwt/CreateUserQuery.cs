using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanVien.Query.Contracts.Errors;
using QuanLyNhanVien.Query.Contracts.Shared;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using QuanLyNhanVien.Query.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.Jwt
{
    public record CreateUserQuery : IRequest<Result<User>>
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; } // Mật khẩu thô, sẽ được mã hóa sau
    }
    public class CreateUserQueryValidator : AbstractValidator<CreateUserQuery>
    {
        private readonly ApplicationDbContext _context;

        public CreateUserQueryValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // Quy tắc xác thực
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
        }
    }

    public class CreateUserQueryHandler : IRequestHandler<CreateUserQuery, Result<User>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateUserQueryValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateUserQueryHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateUserQueryValidator(context);
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<Result<User>> Handle(CreateUserQuery request, CancellationToken cancellationToken)
        {
            if (!_httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false)
            {
                return Result<User>.Failure(new Error("Bạn không có quyền tạo người dùng."));
            }

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<User>.Failure(new Error(errorMessages));
            }

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
                var userRepository = _unitOfWork.Repository<User, int>();
                userRepository.Add(user);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    var createdUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == user.UserId, cancellationToken);

                    if (createdUser == null)
                    {
                        return Result<User>.Failure(new Error("Không thể tìm thấy tài khoản vừa tạo."));
                    }

                    return Result<User>.Success(createdUser);
                }
                transaction.Rollback();
                return Result<User>.Failure(new Error("Không có thay đổi nào được thực hiện khi tạo tài khoản."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<User>.Failure(new Error($"Lỗi khi tạo tài khoản: {ex.Message}"));
            }
        }
    }
}
