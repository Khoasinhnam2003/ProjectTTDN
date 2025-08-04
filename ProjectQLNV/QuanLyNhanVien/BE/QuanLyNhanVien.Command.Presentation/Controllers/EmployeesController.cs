using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IMediator mediator, ILogger<EmployeesController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Employee>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Employee>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeCommand command)
        {
            _logger.LogInformation("Received CreateEmployee request for name: {FirstName} {LastName}", command.FirstName, command.LastName);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created employee with ID: {EmployeeId}", result.Data?.EmployeeId);
                return Ok(result);
            }
            _logger.LogWarning("Failed to create employee with name {FirstName} {LastName}, Error: {Error}", command.FirstName, command.LastName, result.Error.Message);
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
            _logger.LogInformation("Received UpdateEmployee request for employee ID: {EmployeeId}", employeeId);

            command.EmployeeId = employeeId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated employee with ID: {EmployeeId}", employeeId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Nhân viên không tồn tại"))
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found", employeeId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to update employee with ID {EmployeeId}, Error: {Error}", employeeId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteEmployee(int employeeId)
        {
            _logger.LogInformation("Received DeleteEmployee request for employee ID: {EmployeeId}", employeeId);

            var command = new DeleteEmployeeCommand { EmployeeId = employeeId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted employee with ID: {EmployeeId}", employeeId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Nhân viên không tồn tại"))
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found", employeeId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to delete employee with ID {EmployeeId}, Error: {Error}", employeeId, result.Error.Message);
            return BadRequest(result);
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
            _logger.LogInformation("Received UpdateEmployeeSalary request for employee ID: {EmployeeId} with new salary: {NewSalary}", employeeId, command.NewSalary);

            command.EmployeeId = employeeId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated salary for employee ID: {EmployeeId} to {NewSalary}", employeeId, command.NewSalary);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Employee not found."))
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found", employeeId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to update salary for employee ID {EmployeeId}, Error: {Error}", employeeId, result.Error.Message);
            return BadRequest(result);
        }
       }
}
