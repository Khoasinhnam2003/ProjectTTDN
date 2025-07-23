using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using QuanLyNhanVien.Command.Application.UseCases.Attandances;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Response;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Entities;
using QuanLyNhanVien.Command.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuanLyNhanVien.Command.Application.UseCases.Jwt
{
    public record LoginCommand : IRequest<Result<LoginResponse>> // Sử dụng LoginResponse từ LoginResponse.cs
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        private readonly ApplicationDbContext _context;

        public LoginCommandValidator(ApplicationDbContext context)
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

    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(ApplicationDbContext context, IConfiguration configuration, IMediator mediator, ILogger<LoginCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing login request for username: {Username}", request.Username);

            var validator = new LoginCommandValidator(_context);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for login request: {Errors}", errorMessages);
                return Result<LoginResponse>.Failure(new Error(errorMessages));
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Username} not found", request.Username);
                return Result<LoginResponse>.Failure(new Error("Tên đăng nhập không tồn tại."));
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user {Username}", request.Username);
                return Result<LoginResponse>.Failure(new Error("Mật khẩu không đúng."));
            }

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var token = GenerateJwtToken(user, roles);
            _logger.LogInformation("JWT token generated for user {Username}", request.Username);

            DateTime? checkInTime = null;
            if (user.EmployeeId > 0)
            {
                var todayStart = DateTime.Today;
                var todayEnd = todayStart.AddDays(1).AddSeconds(-1);
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeId == user.EmployeeId &&
                                            a.CheckInTime >= todayStart &&
                                            a.CheckInTime <= todayEnd &&
                                            !a.CheckOutTime.HasValue,
                                            cancellationToken);

                if (existingAttendance != null)
                {
                    existingAttendance.CheckInTime = DateTime.Now;
                    existingAttendance.Status = "Present";
                    existingAttendance.UpdatedAt = DateTime.Now;

                    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        _context.Set<Attendance>().Update(existingAttendance);
                        await _context.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync();
                        checkInTime = existingAttendance.CheckInTime;
                        _logger.LogInformation("Check-in updated for EmployeeId {EmployeeId} at {CheckInTime}", user.EmployeeId, checkInTime);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Check-in update failed for EmployeeId {EmployeeId}", user.EmployeeId);
                    }
                }
                else
                {
                    var checkInCommand = new CreateAttendanceCommand
                    {
                        EmployeeId = user.EmployeeId,
                        CheckInTime = DateTime.Now,
                        Status = "Present",
                        Notes = "Auto check-in on login",
                        IsAutoCheckIn = true
                    };
                    try
                    {
                        var result = await _mediator.Send(checkInCommand, cancellationToken);
                        if (result.IsSuccess)
                        {
                            checkInTime = result.Data.CheckInTime;
                            _logger.LogInformation("Check-in recorded for EmployeeId {EmployeeId} at {CheckInTime}", user.EmployeeId, checkInTime);
                        }
                        else
                        {
                            _logger.LogWarning("Check-in failed for EmployeeId {EmployeeId}: {Error}", user.EmployeeId, result.Error?.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Check-in failed for EmployeeId {EmployeeId}", user.EmployeeId);
                    }
                }
            }

            var response = new LoginResponse
            {
                UserId = user.UserId,
                EmployeeId = user.EmployeeId,
                Username = user.Username,
                Token = token,
                Roles = roles,
                CheckInTime = checkInTime
            };

            _logger.LogInformation("Login successful for user {Username} with EmployeeId {EmployeeId}", request.Username, user.EmployeeId);
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
