using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;

        public UsersController(IMediator mediator, ILogger logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersQuery query)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested all users with PageNumber={PageNumber} and PageSize={PageSize}", userId, query.PageNumber, query.PageSize);
            try
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
                _logger.LogInformation("Successfully returned {Count} users for user {UserId}", response.Count, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching all users for user {UserId}", userId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var requestingUserId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {RequestingUserId} requested user with ID {UserId}", requestingUserId, userId);
            try
            {
                var query = new GetUserByIdQuery { UserId = userId };
                var user = await _mediator.Send(query);
                var response = new
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
                };
                _logger.LogInformation("Successfully returned user with ID {UserId} for user {RequestingUserId}", userId, requestingUserId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User with ID {UserId} not found for user {RequestingUserId}", userId, requestingUserId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching user with ID {UserId} for user {RequestingUserId}", userId, requestingUserId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsersByEmployeeName([FromQuery] string name, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested users by employee name with Name={Name}, PageNumber={PageNumber}, PageSize={PageSize}", userId, name, pageNumber, pageSize);
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("Invalid request: Search name is empty for user {UserId}", userId);
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
                _logger.LogInformation("Successfully returned {Count} users for search Name={Name} for user {UserId}", response.Count, name, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching users by employee name for Name={Name} for user {UserId}", name, userId);
                throw;
            }
        }
    }
}
