using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Query.Application.UseCases.Attandances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AttendanceController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllAttendance([FromQuery] GetAllAttendanceQuery query)
        {
            var attendances = await _mediator.Send(query);
            var response = attendances
                .OrderBy(a => a.AttendanceId)
                .Select(a => new
                {
                    attendanceId = a.AttendanceId,
                    employeeId = a.EmployeeId,
                    employeeName = $"{a.Employee?.FirstName} {a.Employee?.LastName}",
                    checkInTime = a.CheckInTime,
                    checkOutTime = a.CheckOutTime,
                    status = a.Status,
                    notes = a.Notes
                }).ToList();
            return Ok(response);
        }

        [Authorize]
        [HttpGet("by-employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAttendancesByEmployee(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var query = new GetAttendancesByEmployeeQuery { EmployeeId = employeeId, PageNumber = pageNumber, PageSize = pageSize };
                var attendances = await _mediator.Send(query);
                var response = attendances
                    .OrderBy(a => a.AttendanceId)
                    .Select(a => new
                    {
                        attendanceId = a.AttendanceId,
                        employeeId = a.EmployeeId,
                        employeeName = $"{a.Employee?.FirstName} {a.Employee?.LastName}",
                        checkInTime = a.CheckInTime,
                        checkOutTime = a.CheckOutTime,
                        status = a.Status,
                        notes = a.Notes
                    }).ToList();
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{attendanceId}/work-hours")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CalculateWorkHours(int attendanceId)
        {
            try
            {
                var query = new CalculateWorkHoursQuery { AttendanceId = attendanceId };
                var workHours = await _mediator.Send(query);
                return Ok(new { attendanceId = attendanceId, workHours = workHours });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
