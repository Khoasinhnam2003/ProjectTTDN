using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public UserRolesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUserRoles([FromQuery] GetAllUserRolesQuery query)
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
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{userId}/{roleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserRoleById(int userId, int roleId)
        {
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
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
