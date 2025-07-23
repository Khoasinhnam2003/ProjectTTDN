using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Application.UseCases.Employees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IMediator mediator, ILogger<EmployeesController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllEmployee([FromQuery] GetAllEmployeesQuery query)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested all employees with PageNumber={PageNumber} and PageSize={PageSize}", userId, query.PageNumber, query.PageSize);
            var employees = await _mediator.Send(query);
            var response = employees
                .OrderBy(e => e.EmployeeId)
                .Select(e => new
                {
                    EmployeeId = e.EmployeeId,
                    FullName = $"{e.FirstName} {e.LastName}",
                    Email = e.Email,
                    DepartmentName = e.Department?.DepartmentName,
                    PositionName = e.Position?.PositionName
                }).ToList();
            _logger.LogInformation("Successfully returned {Count} employees for user {UserId}", response.Count, userId);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeeById(int employeeId)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested employee with ID {EmployeeId}", userId, employeeId);
            try
            {
                var query = new GetEmployeesByIdQuery { EmployeeId = employeeId };
                var employee = await _mediator.Send(query);
                _logger.LogInformation("Successfully returned employee with ID {EmployeeId} for user {UserId}", employeeId, userId);
                return Ok(new
                {
                    EmployeeId = employee.EmployeeId,
                    FullName = $"{employee.FirstName} {employee.LastName}",
                    Email = employee.Email,
                    DepartmentName = employee.Department?.DepartmentName,
                    PositionName = employee.Position?.PositionName
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found for user {UserId}: {Message}", employeeId, userId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("by-department/{departmentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeesByDepartment(int departmentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested employees for DepartmentId={DepartmentId} with PageNumber={PageNumber} and PageSize={PageSize}", userId, departmentId, pageNumber, pageSize);
            var query = new GetEmployeesByDepartmentQuery
            {
                DepartmentId = departmentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var employees = await _mediator.Send(query);
            var response = employees.Select(e => new
            {
                EmployeeId = e.EmployeeId,
                FullName = $"{e.FirstName} {e.LastName}",
                Email = e.Email,
                DepartmentName = e.DepartmentName,
                PositionName = e.PositionName
            }).ToList();
            if (employees.Any())
            {
                _logger.LogInformation("Successfully returned {Count} employees for DepartmentId={DepartmentId} for user {UserId}", response.Count, departmentId, userId);
                return Ok(response);
            }
            _logger.LogWarning("No employees found for DepartmentId={DepartmentId} for user {UserId}", departmentId, userId);
            return NotFound(new { Message = "No employees found in this department." });
        }

        [Authorize(Roles = "Manager,Admin")]
        [HttpGet("by-role/{role}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeesByRole(string role, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10000)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested employees with role {Role} with PageNumber={PageNumber} and PageSize={PageSize}", userId, role, pageNumber, pageSize);

            // Kiểm tra null hoặc rỗng cho tham số role
            if (string.IsNullOrWhiteSpace(role))
            {
                _logger.LogWarning("Invalid role parameter provided by user {UserId}", userId);
                return BadRequest(new { Message = "Role parameter is required." });
            }

            try
            {
                var query = new GetEmployeesByRoleQuery
                {
                    Role = role,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var employees = await _mediator.Send(query);
                var response = employees.Select(e => new
                {
                    EmployeeId = e.EmployeeId,
                    FullName = $"{e.FirstName} {e.LastName}",
                    Email = e.Email,
                    DepartmentName = e.Department?.DepartmentName,
                    PositionName = e.Position?.PositionName
                }).ToList();
                if (employees.Any())
                {
                    _logger.LogInformation("Successfully returned {Count} employees with role {Role} for user {UserId}", response.Count, role, userId);
                    return Ok(new {values = response });
                }
                _logger.LogWarning("No employees found with role {Role} for user {UserId}", role, userId);
                return NotFound(new { Message = $"No employees found with role {role}." });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching employees with role {Role} for user {UserId}: {Message}", role, userId, ex.Message);
                return NotFound(new { Message = $"An error occurred while fetching employees: {ex.Message}" });
            }
        }
    }
}
