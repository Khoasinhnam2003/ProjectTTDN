using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Query.Application.UseCases.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersQuery query)
        {
            var users = await _mediator.Send(query);
            var response = users.Select(u => new
            {
                userId = u.UserId,
                employeeId = u.EmployeeId,
                username = u.Username,
                employeeName = u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}" : null,
                roles = u.UserRoles.Select(ur => new
                {
                    roleId = ur.RoleId,
                    roleName = ur.Role?.RoleName
                }).ToList()
            }).ToList();
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var query = new GetUserByIdQuery { UserId = userId };
                var user = await _mediator.Send(query);
                return Ok(new
                {
                    userId = user.UserId,
                    employeeId = user.EmployeeId,
                    username = user.Username,
                    employeeName = user.Employee != null ? $"{user.Employee.FirstName} {user.Employee.LastName}" : null,
                    roles = user.UserRoles.Select(ur => new
                    {
                        roleId = ur.RoleId,
                        roleName = ur.Role?.RoleName
                    }).ToList()
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsersByEmployeeName([FromQuery] string name, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest(new { Message = "Tên tìm kiếm không được để trống." });
            }

            var query = new GetUsersByEmployeeNameQuery
            {
                Name = name,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var users = await _mediator.Send(query);
            var response = users.Select(u => new
            {
                userId = u.UserId,
                employeeId = u.EmployeeId,
                username = u.Username,
                employeeName = u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}" : null,
                roles = u.UserRoles.Select(ur => new
                {
                    roleId = ur.RoleId,
                    roleName = ur.Role?.RoleName
                }).ToList()
            }).ToList();
            return Ok(response);
        }
    }
}
