using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.Employees
{
    public class GetEmployeesByRoleQuery : IRequest<List<Employee>>
    {
        public string Role { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10000;
    }

    // Xác thực tham số của truy vấn
    public class GetEmployeesByRoleQueryValidator : AbstractValidator<GetEmployeesByRoleQuery>
    {
        public GetEmployeesByRoleQueryValidator()
        {
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Vai trò không được để trống.")
                .Must(role => new[] { "Admin", "Manager", "User" }.Contains(role)).WithMessage("Vai trò phải là Admin, Manager hoặc User.");
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0.")
                .LessThanOrEqualTo(10000).WithMessage("PageSize không được vượt quá 10000.");
        }
    }

    // Xử lý truy vấn lấy nhân viên theo vai trò
    public class GetEmployeesByRoleQueryHandler : IRequestHandler<GetEmployeesByRoleQuery, List<Employee>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetEmployeesByRoleQueryHandler> _logger;

        public GetEmployeesByRoleQueryHandler(IUnitOfWork unitOfWork, ILogger<GetEmployeesByRoleQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Employee>> Handle(GetEmployeesByRoleQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Đang lấy nhân viên với vai trò {Role} với PageNumber={PageNumber} và PageSize={PageSize}", request.Role, request.PageNumber, request.PageSize);

            // Lấy danh sách UserId dựa trên vai trò từ bảng User, UserRole và Role
            var userRepository = _unitOfWork.Repository<User>();
            var usersWithRole = await userRepository.GetAll()
                .Join(_unitOfWork.Repository<UserRole>().GetAll(),
                    user => user.UserId,
                    userRole => userRole.UserId,
                    (user, userRole) => new { User = user, UserRole = userRole })
                .Join(_unitOfWork.Repository<Role>().GetAll(),
                    ur => ur.UserRole.RoleId,
                    role => role.RoleId,
                    (ur, role) => new { ur.User, Role = role })
                .Where(ur => ur.Role.RoleName == request.Role)
                .Select(ur => ur.User)
                .ToListAsync(cancellationToken);

            if (!usersWithRole.Any())
            {
                _logger.LogWarning("Không tìm thấy người dùng nào với vai trò {Role}", request.Role);
                return new List<Employee>();
            }

            var userIds = usersWithRole.Select(u => u.UserId).ToList();
            _logger.LogInformation("Số lượng UserIds tìm thấy với vai trò {Role}: {Count}, UserIds: {UserIds}", request.Role, userIds.Count, string.Join(",", userIds));

            // Lấy danh sách Employee dựa trên EmployeeId trong User
            var employeeRepository = _unitOfWork.Repository<Employee>();
            var employees = await employeeRepository.GetAll()
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Join(userRepository.GetAll(),
                    e => e.EmployeeId,
                    u => u.EmployeeId, // Giả sử User có trường EmployeeId để ánh xạ
                    (e, u) => new { Employee = e, User = u })
                .Where(x => userIds.Contains(x.User.UserId))
                .Select(x => x.Employee)
                .OrderBy(e => e.EmployeeId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Số lượng nhân viên trả về với vai trò {Role}: {Count}, EmployeeIds: {EmployeeIds}", request.Role, employees.Count, string.Join(",", employees.Select(e => e.EmployeeId)));

            return employees;
        }
    }
}
