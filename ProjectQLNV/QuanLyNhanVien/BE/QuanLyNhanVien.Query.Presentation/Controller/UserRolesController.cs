using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Application.UseCases.UserRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRolesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UserRolesController> _logger;

        public UserRolesController(IMediator mediator, ILogger<UserRolesController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUserRoles([FromQuery] GetAllUserRolesQuery query)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested all user roles with PageNumber={PageNumber} and PageSize={PageSize}", userId, query.PageNumber, query.PageSize);
            try
            {
                var userRoles = await _mediator.Send(query);
                var response = userRoles.Select(ur => new
                {
                    userId = ur.UserId,
                    roleId = ur.RoleId,
                    username = ur.User?.Username,
                    employeeName = ur.User?.Employee != null ? $"{ur.User.Employee.FirstName} {ur.User.Employee.LastName}" : null,
                    roleName = ur.Role?.RoleName,
                    createdAt = ur.CreatedAt
                }).ToList();
                _logger.LogInformation("Successfully returned {Count} user roles for user {UserId}", response.Count, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching all user roles for user {UserId}", userId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("{userId}/{roleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserRoleById(int userId, int roleId)
        {
            var requestingUserId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {RequestingUserId} requested user role with UserId={UserId} and RoleId={RoleId}", requestingUserId, userId, roleId);
            try
            {
                var query = new GetUserRoleByIdQuery { UserId = userId, RoleId = roleId };
                var userRole = await _mediator.Send(query);
                var response = new
                {
                    userId = userRole.UserId,
                    roleId = userRole.RoleId,
                    username = userRole.User?.Username,
                    employeeName = userRole.User?.Employee != null ? $"{userRole.User.Employee.FirstName} {userRole.User.Employee.LastName}" : null,
                    roleName = userRole.Role?.RoleName,
                    createdAt = userRole.CreatedAt
                };
                _logger.LogInformation("Successfully returned user role with UserId={UserId} and RoleId={RoleId} for user {RequestingUserId}", userId, roleId, requestingUserId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User role with UserId={UserId} and RoleId={RoleId} not found for user {RequestingUserId}", userId, roleId, requestingUserId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching user role with UserId={UserId} and RoleId={RoleId} for user {RequestingUserId}", userId, roleId, requestingUserId);
                throw;
            }
        }
    }
}
