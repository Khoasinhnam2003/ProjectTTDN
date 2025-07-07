using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Query.Application.UseCases.Employees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllEmployee([FromQuery] GetAllEmployeesQuery query)
        {
            var employees = await _mediator.Send(query);
            var response = employees.Select(e => new
            {
                EmployeeId = e.EmployeeId,
                FullName = $"{e.FirstName} {e.LastName}",
                Email = e.Email,
                DepartmentName = e.Department?.DepartmentName,
                PositionName = e.Position?.PositionName
            }).ToList();
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeeById(int employeeId)
        {
            try
            {
                var query = new GetEmployeeByIdQuery { EmployeeId = employeeId };
                var employee = await _mediator.Send(query);
                return Ok(employee);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("by-department/{departmentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeesByDepartment(int departmentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetEmployeesByDepartmentQuery
            {
                DepartmentId = departmentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var employees = await _mediator.Send(query);
            return employees.Any() ? Ok(employees) : NotFound(new { Message = "No employees found in this department." });
        }
    }
}
