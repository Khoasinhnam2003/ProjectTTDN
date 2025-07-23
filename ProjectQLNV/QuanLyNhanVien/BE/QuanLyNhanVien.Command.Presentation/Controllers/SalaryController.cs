using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public SalaryController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateSalaryHistory([FromBody] CreateSalaryHistoryCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { Message = result.Error.Message });
                }

                var salaryHistory = result.Data;
                return Ok(new
                {
                    SalaryHistoryId = salaryHistory.SalaryHistoryId,
                    EmployeeId = salaryHistory.EmployeeId,
                    FullName = salaryHistory.Employee != null ? $"{salaryHistory.Employee.FirstName} {salaryHistory.Employee.LastName}" : null,
                    Salary = salaryHistory.Salary,
                    EffectiveDate = salaryHistory.EffectiveDate
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{salaryHistoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateSalaryHistory(int salaryHistoryId, [FromBody] UpdateSalaryHistoryCommand command)
        {
            try
            {
                if (salaryHistoryId != command.SalaryHistoryId)
                {
                    return BadRequest(new { Message = "SalaryHistoryId trong URL không khớp với command." });
                }

                var result = await _mediator.Send(command);
                if (!result.IsSuccess)
                {
                    return result.Error.Message.Contains("không tồn tại") ? NotFound(new { Message = result.Error.Message }) : BadRequest(new { Message = result.Error.Message });
                }

                var salaryHistory = result.Data;
                return Ok(new
                {
                    SalaryHistoryId = salaryHistory.SalaryHistoryId,
                    EmployeeId = salaryHistory.EmployeeId,
                    FullName = salaryHistory.Employee != null ? $"{salaryHistory.Employee.FirstName} {salaryHistory.Employee.LastName}" : null,
                    Salary = salaryHistory.Salary,
                    EffectiveDate = salaryHistory.EffectiveDate
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{salaryHistoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteSalaryHistory(int salaryHistoryId)
        {
            try
            {
                var command = new DeleteSalaryHistoryCommand { SalaryHistoryId = salaryHistoryId };
                var result = await _mediator.Send(command);
                if (!result.IsSuccess)
                {
                    return result.Error.Message.Contains("không tồn tại") ? NotFound(new { Message = result.Error.Message }) : BadRequest(new { Message = result.Error.Message });
                }

                return Ok(new { Message = "Xóa lịch sử lương thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
