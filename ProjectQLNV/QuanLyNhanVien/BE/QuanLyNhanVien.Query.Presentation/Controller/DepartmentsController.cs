using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Query.Application.UseCases.Departments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DepartmentsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllDepartments([FromQuery] GetAllDepartmentsQuery query)
        {
            var departments = await _mediator.Send(query);
            var response = departments.Select(d => new
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Location = d.Location,
                ManagerName = d.Manager != null ? $"{d.Manager.FirstName} {d.Manager.LastName}" : null
            }).ToList();
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{departmentId}/employee-count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartmentEmployeeCount(int departmentId)
        {
            try
            {
                var query = new GetDepartmentEmployeeCountQuery { DepartmentId = departmentId };
                var count = await _mediator.Send(query);
                return Ok(new { DepartmentId = departmentId, EmployeeCount = count });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
