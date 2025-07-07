using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuanLyNhanVien.Command.Domain.Response;
using QuanLyNhanVien.Query.Contracts.Errors;
using QuanLyNhanVien.Query.Contracts.Shared;
using QuanLyNhanVien.Query.Domain.Entities;
using QuanLyNhanVien.Query.Persistence;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.Jwt
{
    public record LoginQuery : IRequest<Result<LoginResponse>>
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class LoginQueryValidator : AbstractValidator<LoginQuery>
    {
        private readonly ApplicationDbContext _context;

        public LoginQueryValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Tên đăng nhập không được để trống.")
                .MaximumLength(50)
                .WithMessage("Tên đăng nhập tối đa 50 ký tự.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(8)
                .WithMessage("Mật khẩu phải có ít nhất 8 ký tự.");
        }
    }
    public class LoginQueryHandler : IRequestHandler<LoginQuery, Result<LoginResponse>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public LoginQueryHandler(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<Result<LoginResponse>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            // Thực hiện validation
            var validator = new LoginQueryValidator(_context);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<LoginResponse>.Failure(new Error(errorMessages));
            }

            // Tìm người dùng trong database, bao gồm vai trò
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user == null)
            {
                return Result<LoginResponse>.Failure(new Error("Tên đăng nhập không tồn tại."));
            }

            // Kiểm tra mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Result<LoginResponse>.Failure(new Error("Mật khẩu không đúng."));
            }

            // Lấy danh sách vai trò
            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();

            // Tạo token JWT
            var token = GenerateJwtToken(user, roles);

            // Trả về thông tin đăng nhập
            var response = new LoginResponse
            {
                UserId = user.UserId,
                EmployeeId = user.EmployeeId,
                Username = user.Username,
                Token = token,
                Roles = roles // Thêm vai trò vào response
            };

            return Result<LoginResponse>.Success(response);
        }

        private string GenerateJwtToken(User user, List<string> roles)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("Khóa bí mật JWT không được cấu hình.");
            }

            var key = Encoding.UTF8.GetBytes(secretKey);
            if (key.Length < 32)
            {
                key = key.Concat(new byte[32 - key.Length]).ToArray();
            }

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim("EmployeeId", user.EmployeeId.ToString())
                };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            claims.Add(new Claim(JwtRegisteredClaimNames.Iss, _configuration["Jwt:Issuer"]));
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, _configuration["Jwt:Audience"]));

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
