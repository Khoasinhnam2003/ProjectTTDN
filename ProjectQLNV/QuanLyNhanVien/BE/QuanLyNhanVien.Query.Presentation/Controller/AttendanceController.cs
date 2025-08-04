using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(IMediator mediator, ILogger<AttendanceController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllAttendance([FromQuery] GetAllAttendanceQuery query)
        {
            _logger.LogInformation("Received GetAllAttendance request with PageNumber: {PageNumber}, PageSize: {PageSize}",
                query.PageNumber, query.PageSize);

            try
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

                _logger.LogInformation("Successfully retrieved {Count} attendance records", response.Count);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to retrieve attendance records: {Error}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving attendance records");
                return StatusCode(500, new { Message = "Lỗi không xác định khi lấy danh sách chấm công." });
            }
        }

        [Authorize]
        [HttpGet("by-employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAttendancesByEmployee(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            _logger.LogInformation("Received GetAttendancesByEmployee request for EmployeeId: {EmployeeId}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                employeeId, pageNumber, pageSize);

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

                _logger.LogInformation("Successfully retrieved {Count} attendance records for EmployeeId: {EmployeeId}",
                    response.Count, employeeId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("No attendance records found for EmployeeId: {EmployeeId}, Error: {Error}",
                    employeeId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving attendance records for EmployeeId: {EmployeeId}", employeeId);
                return StatusCode(500, new { Message = "Lỗi không xác định khi lấy chấm công của nhân viên." });
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
            _logger.LogInformation("Received CalculateWorkHours request for AttendanceId: {AttendanceId}", attendanceId);

            try
            {
                var query = new CalculateWorkHoursQuery { AttendanceId = attendanceId };
                var workHours = await _mediator.Send(query);
                _logger.LogInformation("Successfully calculated work hours for AttendanceId: {AttendanceId}: {WorkHours} hours",
                    attendanceId, workHours);
                return Ok(new { attendanceId = attendanceId, workHours = workHours });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Attendance record not found for AttendanceId: {AttendanceId}, Error: {Error}",
                    attendanceId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for AttendanceId: {AttendanceId}, Error: {Error}",
                    attendanceId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calculating work hours for AttendanceId: {AttendanceId}", attendanceId);
                return StatusCode(500, new { Message = "Lỗi không xác định khi tính giờ làm việc." });
            }
        }
    }
}
