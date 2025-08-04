using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Application.UseCases.Departments;
using QuanLyNhanVien.Command.Contracts.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IMediator mediator, ILogger<DepartmentsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentCommand command)
        {
            _logger.LogInformation("Received CreateDepartment request for department name: {DepartmentName}", command.DepartmentName);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created department with name: {DepartmentName}", command.DepartmentName);
                return Ok(result);
            }
            _logger.LogWarning("Failed to create department with name: {DepartmentName}, Error: {Error}", command.DepartmentName, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{departmentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateDepartment(int departmentId, [FromBody] UpdateDepartmentCommand command)
        {
            _logger.LogInformation("Received UpdateDepartment request for department ID: {DepartmentId}", departmentId);

            command.DepartmentId = departmentId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated department with ID: {DepartmentId}", departmentId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Phòng ban không tồn tại"))
            {
                _logger.LogWarning("Department with ID {DepartmentId} not found", departmentId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to update department with ID: {DepartmentId}, Error: {Error}", departmentId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{departmentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteDepartment(int departmentId)
        {
            _logger.LogInformation("Received DeleteDepartment request for department ID: {DepartmentId}", departmentId);

            var command = new DeleteDepartmentCommand { DepartmentId = departmentId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted department with ID: {DepartmentId}", departmentId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Phòng ban không tồn tại"))
            {
                _logger.LogWarning("Department with ID {DepartmentId} not found", departmentId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to delete department with ID: {DepartmentId}, Error: {Error}", departmentId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{departmentId}/transfer-manager")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TransferDepartmentManager(int departmentId, [FromBody] TransferDepartmentManagerCommand command)
        {
            _logger.LogInformation("Received TransferDepartmentManager request for department ID: {DepartmentId} with new manager ID: {NewManagerId}", departmentId, command.NewManagerId);

            command.DepartmentId = departmentId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully transferred manager for department ID: {DepartmentId} to new manager ID: {NewManagerId}", departmentId, command.NewManagerId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Department not found.") || result.Error.Message.Contains("New manager does not exist."))
            {
                _logger.LogWarning("Failed to transfer manager for department ID: {DepartmentId} due to not found", departmentId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to transfer manager for department ID: {DepartmentId}, Error: {Error}", departmentId, result.Error.Message);
            return BadRequest(result);
        }
    }
}
