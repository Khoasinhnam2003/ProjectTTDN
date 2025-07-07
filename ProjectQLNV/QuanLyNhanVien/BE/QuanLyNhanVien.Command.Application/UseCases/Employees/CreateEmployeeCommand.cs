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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Employees
{
    public record CreateEmployeeCommand : IRequest<Result<Employee>>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime HireDate { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
    }

    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateEmployeeCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // Quy tắc với regex hỗ trợ tiếng Việt và kiểm tra trùng lặp
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50).WithMessage("Tên không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Tên chỉ được chứa chữ cái và khoảng trắng.");
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(50).WithMessage("Họ không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Họ chỉ được chứa chữ cái và khoảng trắng.");
            RuleFor(x => x.Email).NotEmpty().MaximumLength(100).EmailAddress().WithMessage("Email không hợp lệ hoặc vượt quá 100 ký tự.");
            RuleFor(x => x.Phone).MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.")
                .Matches(new Regex("^[0-9]+$")).WithMessage("Số điện thoại chỉ được chứa số.");
            RuleFor(x => x.HireDate).NotEmpty().LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày nhận việc không được trong tương lai.");

            // Kiểm tra trùng lặp FirstName + LastName
            RuleFor(x => x).CustomAsync(async (command, context, cancellationToken) =>
            {
                var employees = await _context.Employees.ToListAsync(cancellationToken);
                var fullName = $"{command.FirstName} {command.LastName}".Trim();
                var duplicate = employees.FirstOrDefault(e =>
                    $"{e.FirstName} {e.LastName}".Trim() == fullName);

                if (duplicate != null)
                {
                    context.AddFailure("Tên đầy đủ đã tồn tại với một nhân viên khác.");
                }
            });

            // Kiểm tra trùng lặp Phone
            RuleFor(x => x.Phone).CustomAsync(async (phone, context, cancellationToken) =>
            {
                var employees = await _context.Employees.ToListAsync(cancellationToken);
                var duplicate = employees.FirstOrDefault(e => e.Phone == phone);

                if (duplicate != null)
                {
                    context.AddFailure("Số điện thoại đã được sử dụng bởi một nhân viên khác.");
                }
            });

            // Kiểm tra trùng lặp Email
            RuleFor(x => x.Email).CustomAsync(async (email, context, cancellationToken) =>
            {
                var employees = await _context.Employees.ToListAsync(cancellationToken);
                var duplicate = employees.FirstOrDefault(e => e.Email == email);

                if (duplicate != null)
                {
                    context.AddFailure("Email đã được sử dụng bởi một nhân viên khác.");
                }
            });

            // Kiểm tra DepartmentId và PositionId tồn tại
            RuleFor(x => x.DepartmentId).CustomAsync(async (deptId, context, cancellationToken) =>
            {
                if (deptId.HasValue)
                {
                    var department = await _context.Departments.FindAsync(new object[] { deptId }, cancellationToken);
                    if (department == null)
                    {
                        context.AddFailure("DepartmentId không tồn tại.");
                    }
                }
            });

            RuleFor(x => x.PositionId).CustomAsync(async (posId, context, cancellationToken) =>
            {
                if (posId.HasValue)
                {
                    var position = await _context.Positions.FindAsync(new object[] { posId }, cancellationToken);
                    if (position == null)
                    {
                        context.AddFailure("PositionId không tồn tại.");
                    }
                }
            });
        }
    }

    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Employee>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateEmployeeCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public CreateEmployeeCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateEmployeeCommandValidator(context); // Truyền context vào validator
        }

        public async Task<Result<Employee>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            // Thực hiện validation
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<Employee>.Failure(new Error(errorMessages));
            }

            var employee = new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth,
                HireDate = request.HireDate,
                DepartmentId = request.DepartmentId,
                PositionId = request.PositionId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var employeeRepository = _unitOfWork.Repository<Employee, int>();
                employeeRepository.Add(employee);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();

                    // Truy vấn lại employee để lấy ID và tải Department/Position
                    var createdEmployee = await _context.Employees
                        .Include(e => e.Department)
                        .Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.EmployeeId == employee.EmployeeId, cancellationToken);

                    if (createdEmployee == null)
                    {
                        return Result<Employee>.Failure(new Error("Không thể tìm thấy nhân viên vừa tạo."));
                    }

                    // Tạo bản sao với thông tin chi tiết
                    var resultEmployee = new Employee
                    {
                        EmployeeId = createdEmployee.EmployeeId,
                        FirstName = createdEmployee.FirstName,
                        LastName = createdEmployee.LastName,
                        Email = createdEmployee.Email,
                        Phone = createdEmployee.Phone,
                        DateOfBirth = createdEmployee.DateOfBirth,
                        HireDate = createdEmployee.HireDate,
                        DepartmentId = createdEmployee.DepartmentId,
                        PositionId = createdEmployee.PositionId,
                        IsActive = createdEmployee.IsActive,
                        CreatedAt = createdEmployee.CreatedAt,
                        UpdatedAt = createdEmployee.UpdatedAt,
                        Department = createdEmployee.Department != null ? new Department
                        {
                            DepartmentId = createdEmployee.Department.DepartmentId,
                            DepartmentName = createdEmployee.Department.DepartmentName,
                            Location = createdEmployee.Department.Location,
                            ManagerId = createdEmployee.Department.ManagerId,
                            CreatedAt = createdEmployee.Department.CreatedAt,
                            UpdatedAt = createdEmployee.Department.UpdatedAt
                        } : null,
                        Position = createdEmployee.Position != null ? new Position
                        {
                            PositionId = createdEmployee.Position.PositionId,
                            PositionName = createdEmployee.Position.PositionName,
                            Description = createdEmployee.Position.Description,
                            BaseSalary = createdEmployee.Position.BaseSalary,
                            CreatedAt = createdEmployee.Position.CreatedAt,
                            UpdatedAt = createdEmployee.Position.UpdatedAt
                        } : null,
                        Attendances = createdEmployee.Attendances,
                        Contracts = createdEmployee.Contracts,
                        Departments = createdEmployee.Departments,
                        SalaryHistories = createdEmployee.SalaryHistories,
                        Skills = createdEmployee.Skills
                    };
                    return Result<Employee>.Success(resultEmployee);
                }
                transaction.Rollback();
                return Result<Employee>.Failure(new Error("Không có thay đổi nào được thực hiện khi tạo nhân viên."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<Employee>.Failure(new Error($"Lỗi khi tạo nhân viên: {ex.Message}"));
            }
        }
    }
}
