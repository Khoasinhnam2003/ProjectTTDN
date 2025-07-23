using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Command.Application.UseCases.Roles;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public RolesController(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Role>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Role>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{roleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Role>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Role>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Role>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateRole(int roleId, [FromBody] UpdateRoleCommand command)
        {
            if (roleId != command.RoleId)
            {
                return BadRequest(Result<Role>.Failure(new Error("RoleId trong URL và body phải khớp.")));
            }
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return result.Error.Message.Contains("không tồn tại") ? NotFound(result) : BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{roleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            var command = new DeleteRoleCommand { RoleId = roleId };
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return result.Error.Message.Contains("không tồn tại") ? NotFound(result) : BadRequest(result);
        }
    }
}
