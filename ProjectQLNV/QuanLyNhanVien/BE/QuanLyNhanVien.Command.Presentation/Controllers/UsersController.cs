using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Application.UseCases.Users;
using QuanLyNhanVien.Command.Contracts.Errors;
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
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IMediator mediator, ILogger<UsersController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
        {
            _logger.LogInformation("Received CreateUser request for username: {Username}, EmployeeId: {EmployeeId}",
                command.Username, command.EmployeeId);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created user with ID: {UserId}, Username: {Username}",
                    result.Data.UserId, command.Username);
                return Ok(result);
            }

            _logger.LogWarning("Failed to create user with username {Username}, Error: {Error}",
                command.Username, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPut("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserCommand command)
        {
            _logger.LogInformation("Received UpdateUser request for user ID: {UserId}", userId);

            if (userId != command.UserId)
            {
                _logger.LogWarning("UserId mismatch: URL UserId {UrlUserId} does not match body UserId {BodyUserId}",
                    userId, command.UserId);
                return BadRequest(Result<User>.Failure(new Error("UserId trong URL và body phải khớp.")));
            }

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated user with ID: {UserId}", userId);
                return Ok(result);
            }

            if (result.Error.Message.Contains("không tồn tại"))
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return NotFound(result);
            }

            _logger.LogWarning("Failed to update user with ID {UserId}, Error: {Error}", userId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            _logger.LogInformation("Received DeleteUser request for user ID: {UserId}", userId);

            var command = new DeleteUserCommand { UserId = userId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted user with ID: {UserId}", userId);
                return Ok(result);
            }

            if (result.Error.Message.Contains("không tồn tại"))
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return NotFound(result);
            }

            _logger.LogWarning("Failed to delete user with ID {UserId}, Error: {Error}", userId, result.Error.Message);
            return BadRequest(result);
        }
    }
}
