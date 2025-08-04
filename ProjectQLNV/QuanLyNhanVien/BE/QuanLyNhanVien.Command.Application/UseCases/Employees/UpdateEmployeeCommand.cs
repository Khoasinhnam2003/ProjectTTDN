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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Employees
{
    public record UpdateEmployeeCommand : IRequest<Result<Employee>>
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime HireDate { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateEmployeeCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("ID nhân viên phải lớn hơn 0.");
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50).WithMessage("Tên không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Tên chỉ được chứa chữ cái và khoảng trắng.");
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(50).WithMessage("Họ không được để trống và tối đa 50 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Họ chỉ được chứa chữ cái và khoảng trắng.");
            RuleFor(x => x.Email).NotEmpty().MaximumLength(100).EmailAddress().WithMessage("Email không hợp lệ hoặc vượt quá 100 ký tự.");
            RuleFor(x => x.Phone).MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.")
                .Matches(new Regex("^[0-9]+$")).WithMessage("Số điện thoại chỉ được chứa số.");
            RuleFor(x => x.HireDate).NotEmpty().LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày nhận việc không được trong tương lai.");

            RuleFor(x => x).CustomAsync(async (command, context, cancellationToken) =>
            {
                var employees = await _context.Employees.ToListAsync(cancellationToken);
                var fullName = $"{command.FirstName} {command.LastName}".Trim();
                var duplicate = employees.FirstOrDefault(e =>
                    e.EmployeeId != command.EmployeeId &&
                    $"{e.FirstName} {e.LastName}".Trim() == fullName);

                if (duplicate != null)
                {
                    context.AddFailure("Tên đầy đủ đã tồn tại với một nhân viên khác.");
                }
            });

            RuleFor(x => x.Phone).CustomAsync(async (phone, context, cancellationToken) =>
            {
                var command = (UpdateEmployeeCommand)context.InstanceToValidate;
                var employees = await _context.Employees.ToListAsync(cancellationToken);
                var duplicate = employees.FirstOrDefault(e =>
                    e.EmployeeId != command.EmployeeId &&
                    e.Phone == phone);

                if (duplicate != null)
                {
                    context.AddFailure("Số điện thoại đã được sử dụng bởi một nhân viên khác.");
                }
            });
        }
    }

    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result<Employee>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateEmployeeCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

        public UpdateEmployeeCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<UpdateEmployeeCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateEmployeeCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Employee>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting update for employee with ID: {EmployeeId}", request.EmployeeId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for employee ID {EmployeeId}: {Errors}", request.EmployeeId, errorMessages);
                return Result<Employee>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var employeeRepository = _unitOfWork.Repository<Employee, int>();
                var departmentRepository = _unitOfWork.Repository<Department, int>();
                var positionRepository = _unitOfWork.Repository<Position, int>();

                var employee = await employeeRepository.FindAsync(request.EmployeeId, cancellationToken);
                if (employee == null)
                {
                    transaction.Rollback();
                    _logger.LogWarning("Employee with ID {EmployeeId} not found", request.EmployeeId);
                    return Result<Employee>.Failure(new Error("Nhân viên không tồn tại."));
                }

                Department primaryDepartment = null;
                if (request.DepartmentId.HasValue)
                {
                    primaryDepartment = await departmentRepository.FindAsync(request.DepartmentId.Value, cancellationToken);
                    if (primaryDepartment == null)
                    {
                        transaction.Rollback();
                        _logger.LogWarning("Department with ID {DepartmentId} not found for employee ID {EmployeeId}", request.DepartmentId, request.EmployeeId);
                        return Result<Employee>.Failure(new Error("Phòng ban không tồn tại."));
                    }
                    employee.DepartmentId = primaryDepartment.DepartmentId;
                    employee.Department = primaryDepartment;

                    if (request.PositionId.HasValue && request.PositionId.Value == 2 && primaryDepartment.ManagerId != employee.EmployeeId)
                    {
                        primaryDepartment.ManagerId = employee.EmployeeId;
                        primaryDepartment.UpdatedAt = DateTime.Now;
                        departmentRepository.Update(primaryDepartment);
                        _logger.LogInformation("Updated ManagerId = {ManagerId} for DepartmentId = {DepartmentId}", employee.EmployeeId, primaryDepartment.DepartmentId);
                    }
                    else if (primaryDepartment.ManagerId == employee.EmployeeId && (!request.PositionId.HasValue || request.PositionId.Value != 2))
                    {
                        primaryDepartment.ManagerId = null;
                        primaryDepartment.UpdatedAt = DateTime.Now;
                        departmentRepository.Update(primaryDepartment);
                        _logger.LogInformation("Set ManagerId = NULL for DepartmentId = {DepartmentId}", primaryDepartment.DepartmentId);
                    }
                }

                if (request.PositionId.HasValue)
                {
                    var position = await positionRepository.FindAsync(request.PositionId.Value, cancellationToken);
                    if (position == null)
                    {
                        transaction.Rollback();
                        _logger.LogWarning("Position with ID {PositionId} not found for employee ID {EmployeeId}", request.PositionId, request.EmployeeId);
                        return Result<Employee>.Failure(new Error("Vị trí không tồn tại."));
                    }
                    employee.PositionId = position.PositionId;
                    employee.Position = position;
                }

                employee.FirstName = request.FirstName;
                employee.LastName = request.LastName;
                employee.Email = request.Email;
                employee.Phone = request.Phone;
                employee.DateOfBirth = request.DateOfBirth;
                employee.HireDate = request.HireDate;
                employee.IsActive = request.IsActive ?? employee.IsActive;
                employee.UpdatedAt = DateTime.Now;

                employeeRepository.Update(employee);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    var managedDepartments = await _context.Departments
                        .Where(d => d.ManagerId == employee.EmployeeId)
                        .Include(d => d.Manager)
                        .ToListAsync(cancellationToken);

                    var resultEmployee = new Employee
                    {
                        EmployeeId = employee.EmployeeId,
                        FirstName = employee.FirstName,
                        LastName = employee.LastName,
                        Email = employee.Email,
                        Phone = employee.Phone,
                        DateOfBirth = employee.DateOfBirth,
                        HireDate = employee.HireDate,
                        DepartmentId = employee.DepartmentId,
                        PositionId = employee.PositionId,
                        IsActive = employee.IsActive,
                        CreatedAt = employee.CreatedAt,
                        UpdatedAt = employee.UpdatedAt,
                        Department = primaryDepartment != null ? new Department
                        {
                            DepartmentId = primaryDepartment.DepartmentId,
                            DepartmentName = primaryDepartment.DepartmentName,
                            Location = primaryDepartment.Location,
                            ManagerId = primaryDepartment.ManagerId,
                            CreatedAt = primaryDepartment.CreatedAt,
                            UpdatedAt = primaryDepartment.UpdatedAt,
                            Manager = primaryDepartment.Manager != null ? new Employee
                            {
                                EmployeeId = primaryDepartment.Manager.EmployeeId,
                                FirstName = primaryDepartment.Manager.FirstName,
                                LastName = primaryDepartment.Manager.LastName,
                                Email = primaryDepartment.Manager.Email,
                                Phone = primaryDepartment.Manager.Phone,
                                DateOfBirth = primaryDepartment.Manager.DateOfBirth,
                                HireDate = primaryDepartment.Manager.HireDate,
                                UpdatedAt = primaryDepartment.Manager.UpdatedAt
                            } : null
                        } : null,
                        Departments = managedDepartments.Select(d => new Department
                        {
                            DepartmentId = d.DepartmentId,
                            DepartmentName = d.DepartmentName,
                            Location = d.Location,
                            ManagerId = d.ManagerId,
                            CreatedAt = d.CreatedAt,
                            UpdatedAt = d.UpdatedAt,
                            Manager = d.Manager != null ? new Employee
                            {
                                EmployeeId = d.Manager.EmployeeId,
                                FirstName = d.Manager.FirstName,
                                LastName = d.Manager.LastName,
                                Email = d.Manager.Email,
                                Phone = d.Manager.Phone,
                                DateOfBirth = d.Manager.DateOfBirth,
                                HireDate = d.Manager.HireDate,
                                UpdatedAt = d.Manager.UpdatedAt
                            } : null
                        }).ToList(),
                        Position = employee.Position != null ? new Position
                        {
                            PositionId = employee.Position.PositionId,
                            PositionName = employee.Position.PositionName,
                            Description = employee.Position.Description,
                            BaseSalary = employee.Position.BaseSalary,
                            CreatedAt = employee.Position.CreatedAt,
                            UpdatedAt = employee.Position.UpdatedAt
                        } : null,
                        Attendances = employee.Attendances,
                        Contracts = employee.Contracts,
                        SalaryHistories = employee.SalaryHistories,
                        Skills = employee.Skills
                    };
                    _logger.LogInformation("Successfully updated employee with ID: {EmployeeId}", request.EmployeeId);
                    return Result<Employee>.Success(resultEmployee);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when updating employee with ID: {EmployeeId}", request.EmployeeId);
                return Result<Employee>.Failure(new Error("Không có thay đổi nào được thực hiện khi cập nhật nhân viên."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex.InnerException?.Message.Contains("UNIQUE KEY constraint") == true)
                {
                    _logger.LogWarning("Email conflict for employee ID {EmployeeId}", request.EmployeeId);
                    return Result<Employee>.Failure(new Error("Email đã được sử dụng bởi một nhân viên khác."));
                }
                if (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
                {
                    _logger.LogWarning("Invalid department or position for employee ID {EmployeeId}", request.EmployeeId);
                    return Result<Employee>.Failure(new Error("Phòng ban hoặc vị trí không hợp lệ."));
                }
                _logger.LogError(ex, "Error updating employee with ID: {EmployeeId}", request.EmployeeId);
                return Result<Employee>.Failure(new Error($"Lỗi khi cập nhật nhân viên: {ex.Message}"));
            }
        }
    }
}
