using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public UsersController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<User>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserCommand command)
        {
            if (userId != command.UserId)
            {
                return BadRequest(Result<User>.Failure(new Error("UserId trong URL và body phải khớp.")));
            }
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return result.Error.Message.Contains("không tồn tại") ? NotFound(result) : BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var command = new DeleteUserCommand { UserId = userId };
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return result.Error.Message.Contains("không tồn tại") ? NotFound(result) : BadRequest(result);
        }
    }
}
