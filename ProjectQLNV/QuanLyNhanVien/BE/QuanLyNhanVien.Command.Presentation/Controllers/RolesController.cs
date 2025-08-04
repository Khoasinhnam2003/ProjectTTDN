using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<RolesController> _logger;

        public RolesController(IUnitOfWork unitOfWork, IMediator mediator, ILogger<RolesController> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Role>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Role>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
        {
            _logger.LogInformation("Received CreateRole request for role name: {RoleName}", command.RoleName);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created role with name: {RoleName}, ID: {RoleId}", command.RoleName, result.Data?.RoleId);
                return Ok(result);
            }
            _logger.LogWarning("Failed to create role with name {RoleName}, Error: {Error}", command.RoleName, result.Error.Message);
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
            _logger.LogInformation("Received UpdateRole request for role ID: {RoleId}", roleId);

            if (roleId != command.RoleId)
            {
                _logger.LogWarning("RoleId mismatch: URL RoleId {UrlRoleId} does not match body RoleId {BodyRoleId}", roleId, command.RoleId);
                return BadRequest(Result<Role>.Failure(new Error("RoleId trong URL và body phải khớp.")));
            }

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated role with ID: {RoleId}", roleId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("không tồn tại"))
            {
                _logger.LogWarning("Role with ID {RoleId} not found", roleId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to update role with ID {RoleId}, Error: {Error}", roleId, result.Error.Message);
            return BadRequest(result);
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
            _logger.LogInformation("Received DeleteRole request for role ID: {RoleId}", roleId);

            var command = new DeleteRoleCommand { RoleId = roleId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted role with ID: {RoleId}", roleId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("không tồn tại"))
            {
                _logger.LogWarning("Role with ID {RoleId} not found", roleId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to delete role with ID {RoleId}, Error: {Error}", roleId, result.Error.Message);
            return BadRequest(result);
        }
    }
}
