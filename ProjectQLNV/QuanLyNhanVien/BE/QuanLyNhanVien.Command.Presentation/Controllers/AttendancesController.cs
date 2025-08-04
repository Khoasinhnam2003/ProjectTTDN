using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Application.UseCases.Attandances;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendancesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AttendancesController> _logger;

        public AttendancesController(IMediator mediator, ILogger<AttendancesController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAttendance([FromBody] CreateAttendanceCommand command)
        {
            _logger.LogInformation("Received CreateAttendance request for EmployeeId: {EmployeeId}", command.EmployeeId);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created attendance for EmployeeId: {EmployeeId} with ID: {AttendanceId}",
                    command.EmployeeId, result.Data?.AttendanceId);
                return Ok(result);
            }
            _logger.LogWarning("Failed to create attendance for EmployeeId: {EmployeeId}, Error: {Error}",
                command.EmployeeId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{attendanceId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateAttendance(int attendanceId, [FromBody] UpdateAttendanceCommand command)
        {
            _logger.LogInformation("Received UpdateAttendance request for AttendanceId: {AttendanceId}", attendanceId);

            command.AttendanceId = attendanceId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated attendance with ID: {AttendanceId}", attendanceId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Bản ghi điểm danh không tồn tại"))
            {
                _logger.LogWarning("Attendance with ID {AttendanceId} not found", attendanceId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to update attendance with ID: {AttendanceId}, Error: {Error}",
                attendanceId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{attendanceId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteAttendance(int attendanceId)
        {
            _logger.LogInformation("Received DeleteAttendance request for AttendanceId: {AttendanceId}", attendanceId);

            var command = new DeleteAttendanceCommand { AttendanceId = attendanceId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted attendance with ID: {AttendanceId}", attendanceId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Bản ghi điểm danh không tồn tại"))
            {
                _logger.LogWarning("Attendance with ID {AttendanceId} not found", attendanceId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to delete attendance with ID: {AttendanceId}, Error: {Error}",
                attendanceId, result.Error.Message);
            return BadRequest(result);
        }
    }
}
