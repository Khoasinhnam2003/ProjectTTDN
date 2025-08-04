using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Application.UseCases.SalaryHistories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Presentation.Controllers
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
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateSalaryHistory([FromBody] CreateSalaryHistoryCommand command)
        {
            _logger.LogInformation("Received CreateSalaryHistory request for employee ID: {EmployeeId}, Salary: {Salary}, EffectiveDate: {EffectiveDate}",
                command.EmployeeId, command.Salary, command.EffectiveDate);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                var salaryHistory = result.Data;
                _logger.LogInformation("Successfully created salary history with ID: {SalaryHistoryId} for employee ID: {EmployeeId}",
                    salaryHistory.SalaryHistoryId, salaryHistory.EmployeeId);
                return Ok(new
                {
                    SalaryHistoryId = salaryHistory.SalaryHistoryId,
                    EmployeeId = salaryHistory.EmployeeId,
                    FullName = salaryHistory.Employee != null ? $"{salaryHistory.Employee.FirstName} {salaryHistory.Employee.LastName}" : null,
                    Salary = salaryHistory.Salary,
                    EffectiveDate = salaryHistory.EffectiveDate
                });
            }

            _logger.LogWarning("Failed to create salary history for employee ID {EmployeeId}, Error: {Error}",
                command.EmployeeId, result.Error.Message);
            return BadRequest(new { Message = result.Error.Message });
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPut("{salaryHistoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateSalaryHistory(int salaryHistoryId, [FromBody] UpdateSalaryHistoryCommand command)
        {
            _logger.LogInformation("Received UpdateSalaryHistory request for salary history ID: {SalaryHistoryId}", salaryHistoryId);

            if (salaryHistoryId != command.SalaryHistoryId)
            {
                _logger.LogWarning("SalaryHistoryId mismatch: URL SalaryHistoryId {UrlSalaryHistoryId} does not match body SalaryHistoryId {BodySalaryHistoryId}",
                    salaryHistoryId, command.SalaryHistoryId);
                return BadRequest(new { Message = "SalaryHistoryId trong URL không khớp với command." });
            }

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                var salaryHistory = result.Data;
                _logger.LogInformation("Successfully updated salary history with ID: {SalaryHistoryId}", salaryHistory.SalaryHistoryId);
                return Ok(new
                {
                    SalaryHistoryId = salaryHistory.SalaryHistoryId,
                    EmployeeId = salaryHistory.EmployeeId,
                    FullName = salaryHistory.Employee != null ? $"{salaryHistory.Employee.FirstName} {salaryHistory.Employee.LastName}" : null,
                    Salary = salaryHistory.Salary,
                    EffectiveDate = salaryHistory.EffectiveDate
                });
            }

            if (result.Error.Message.Contains("không tồn tại"))
            {
                _logger.LogWarning("Salary history with ID {SalaryHistoryId} not found", salaryHistoryId);
                return NotFound(new { Message = result.Error.Message });
            }

            _logger.LogWarning("Failed to update salary history with ID {SalaryHistoryId}, Error: {Error}",
                salaryHistoryId, result.Error.Message);
            return BadRequest(new { Message = result.Error.Message });
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{salaryHistoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteSalaryHistory(int salaryHistoryId)
        {
            _logger.LogInformation("Received DeleteSalaryHistory request for salary history ID: {SalaryHistoryId}", salaryHistoryId);

            var command = new DeleteSalaryHistoryCommand { SalaryHistoryId = salaryHistoryId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted salary history with ID: {SalaryHistoryId}", salaryHistoryId);
                return Ok(new { Message = "Xóa lịch sử lương thành công." });
            }

            if (result.Error.Message.Contains("không tồn tại"))
            {
                _logger.LogWarning("Salary history with ID {SalaryHistoryId} not found", salaryHistoryId);
                return NotFound(new { Message = result.Error.Message });
            }

            _logger.LogWarning("Failed to delete salary history with ID {SalaryHistoryId}, Error: {Error}",
                salaryHistoryId, result.Error.Message);
            return BadRequest(new { Message = result.Error.Message });
        }
    }
}
