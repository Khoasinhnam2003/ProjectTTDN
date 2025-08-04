using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Application.UseCases.SalaryHistories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalaryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SalaryController> _logger;

        public SalaryController(IMediator mediator, ILogger<SalaryController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllSalaryHistories([FromQuery] GetAllSalaryHistoriesQuery query)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested all salary histories with PageNumber={PageNumber} and PageSize={PageSize}", userId, query.PageNumber, query.PageSize);
            try
            {
                var salaryHistories = await _mediator.Send(query);
                var response = salaryHistories.Select(sh => new
                {
                    SalaryHistoryId = sh.SalaryHistoryId,
                    EmployeeId = sh.EmployeeId,
                    EmployeeName = sh.Employee != null ? $"{sh.Employee.FirstName} {sh.Employee.LastName}" : null,
                    Salary = sh.Salary,
                    EffectiveDate = sh.EffectiveDate
                }).ToList();
                _logger.LogInformation("Successfully returned {Count} salary histories for user {UserId}", response.Count, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching all salary histories for user {UserId}", userId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("by-employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSalaryHistoriesByEmployee(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested salary histories for EmployeeId={EmployeeId} with PageNumber={PageNumber} and PageSize={PageSize}", userId, employeeId, pageNumber, pageSize);
            try
            {
                var query = new GetSalaryHistoriesByEmployeeQuery
                {
                    EmployeeId = employeeId,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var salaryHistories = await _mediator.Send(query);
                var response = salaryHistories.Select(sh => new
                {
                    SalaryHistoryId = sh.SalaryHistoryId,
                    EmployeeId = sh.EmployeeId,
                    EmployeeName = sh.EmployeeName,
                    Salary = sh.Salary,
                    EffectiveDate = sh.EffectiveDate,
                    CreatedAt = sh.CreatedAt,
                    UpdatedAt = sh.UpdatedAt
                }).ToList();
                if (salaryHistories.Any())
                {
                    _logger.LogInformation("Successfully returned {Count} salary histories for EmployeeId={EmployeeId} for user {UserId}", response.Count, employeeId, userId);
                    return Ok(response);
                }
                _logger.LogWarning("No salary histories found for EmployeeId={EmployeeId} for user {UserId}", employeeId, userId);
                return NotFound(new { Message = "No salary histories found for this employee." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching salary histories for EmployeeId={EmployeeId} for user {UserId}", employeeId, userId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("{salaryHistoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSalaryHistoryById(int salaryHistoryId)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested salary history with ID {SalaryHistoryId}", userId, salaryHistoryId);
            try
            {
                var query = new GetSalaryHistoryByIdQuery { SalaryHistoryId = salaryHistoryId };
                var salaryHistory = await _mediator.Send(query);
                var response = new
                {
                    SalaryHistoryId = salaryHistory.SalaryHistoryId,
                    EmployeeId = salaryHistory.EmployeeId,
                    EmployeeName = salaryHistory.Employee != null ? $"{salaryHistory.Employee.FirstName} {salaryHistory.Employee.LastName}" : null,
                    Salary = salaryHistory.Salary,
                    EffectiveDate = salaryHistory.EffectiveDate,
                    CreatedAt = salaryHistory.CreatedAt,
                    UpdatedAt = salaryHistory.UpdatedAt
                };
                _logger.LogInformation("Successfully returned salary history with ID {SalaryHistoryId} for user {UserId}", salaryHistoryId, userId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Salary history with ID {SalaryHistoryId} not found for user {UserId}", salaryHistoryId, userId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching salary history with ID {SalaryHistoryId} for user {UserId}", salaryHistoryId, userId);
                throw;
            }
        }
    }
}
