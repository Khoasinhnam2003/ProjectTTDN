using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Command.Application.UseCases.Employees;
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
    public class EmployeesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Employee>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Employee>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Employee>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Employee>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Employee>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateEmployee(int employeeId, [FromBody] UpdateEmployeeCommand command)
        {
            command.EmployeeId = employeeId;
            var result = await _mediator.Send(command, CancellationToken.None);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Nhân viên không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteEmployee(int employeeId)
        {
            var command = new DeleteEmployeeCommand { EmployeeId = employeeId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return result.Error.Message.Contains("Nhân viên không tồn tại")
                ? NotFound(result)
                : BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{employeeId}/salary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateEmployeeSalary(int employeeId, [FromBody] UpdateEmployeeSalaryCommand command)
        {
            command.EmployeeId = employeeId;
            var result = await _mediator.Send(command, CancellationToken.None);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Employee not found.")
                    ? NotFound(result)
                    : BadRequest(result);
        }
    }
}
