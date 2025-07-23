using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Application.UseCases.Attandances;
using QuanLyNhanVien.Command.Application.UseCases.Jwt;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Response;
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
    public class AuthenticationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IMediator mediator, ILogger<AuthenticationController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<LoginResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<LoginResponse>))]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            if (command == null)
            {
                _logger.LogWarning("Login attempt with null command");
                return BadRequest(Result<LoginResponse>.Failure(new Error("Yêu cầu không hợp lệ.")));
            }

            _logger.LogInformation("Login attempt for user with credentials: {@Command}", command);
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            _logger.LogWarning("Login failed for user with credentials: {@Command}", command);
            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Attendance>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
            if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int employeeId))
            {
                _logger.LogWarning("Logout attempt failed: Unable to determine EmployeeId from token");
                return Unauthorized(Result<Attendance>.Failure(new Error("Không thể xác định EmployeeId từ token.")));
            }

            _logger.LogInformation("Logout attempt for EmployeeId: {EmployeeId}", employeeId);
            var command = new CreateCheckOutCommand { EmployeeId = employeeId };
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Logout successful for EmployeeId: {EmployeeId}", employeeId);
                return Ok(result);
            }
            _logger.LogWarning("Logout failed for EmployeeId: {EmployeeId}", employeeId);
            return BadRequest(result);
        }
    }
}
