using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public SalaryController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllSalaryHistories([FromQuery] GetAllSalaryHistoriesQuery query)
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
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("by-employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSalaryHistoriesByEmployee(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
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
            return salaryHistories.Any() ? Ok(response) : NotFound(new { Message = "No salary histories found for this employee." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{salaryHistoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSalaryHistoryById(int salaryHistoryId)
        {
            try
            {
                var query = new GetSalaryHistoryByIdQuery { SalaryHistoryId = salaryHistoryId };
                var salaryHistory = await _mediator.Send(query);
                return Ok(new
                {
                    SalaryHistoryId = salaryHistory.SalaryHistoryId,
                    EmployeeId = salaryHistory.EmployeeId,
                    EmployeeName = salaryHistory.Employee != null ? $"{salaryHistory.Employee.FirstName} {salaryHistory.Employee.LastName}" : null,
                    Salary = salaryHistory.Salary,
                    EffectiveDate = salaryHistory.EffectiveDate,
                    CreatedAt = salaryHistory.CreatedAt,
                    UpdatedAt = salaryHistory.UpdatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
