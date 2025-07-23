using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public AttendancesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAttendance([FromBody] CreateAttendanceCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
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
            command.AttendanceId = attendanceId;
            var result = await _mediator.Send(command, CancellationToken.None);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Bản ghi điểm danh không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
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
            var command = new DeleteAttendanceCommand { AttendanceId = attendanceId };
            var result = await _mediator.Send(command);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Bản ghi điểm danh không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
        }
    }
}
