using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Query.Application.UseCases.Roles;
using QuanLyNhanVien.Query.Contracts.Shared;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
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
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRoles([FromQuery] GetAllRolesQuery query)
        {
            var roles = await _mediator.Send(query);
            var response = roles.Select(r => new
            {
                roleId = r.RoleId,
                roleName = r.RoleName,
                description = r.Description
            }).ToList();
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{roleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoleById(int roleId)
        {
            try
            {
                var query = new GetRolesByIdQuery { RoleId = roleId };
                var role = await _mediator.Send(query);
                var response = new
                {
                    roleId = role.RoleId,
                    roleName = role.RoleName,
                    description = role.Description
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
