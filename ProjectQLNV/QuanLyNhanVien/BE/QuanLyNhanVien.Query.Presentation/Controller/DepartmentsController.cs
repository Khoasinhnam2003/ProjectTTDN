using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;

        public DepartmentsController(IMediator mediator, ILogger logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllDepartments([FromQuery] GetAllDepartmentsQuery query)
        {
            _logger.LogInformation("Received request to get all departments, PageNumber: {PageNumber}, PageSize: {PageSize}",
                query.PageNumber, query.PageSize);
            try
            {
                var departments = await _mediator.Send(query);
                var response = departments.Select(d => new
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    Location = d.Location,
                    ManagerName = d.ManagerName,
                    EmployeeCount = d.EmployeeCount
                }).ToList();
                _logger.LogInformation("Successfully retrieved {DepartmentCount} departments", response.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving all departments");
                throw;
            }
        }

        [Authorize]
        [HttpGet("{departmentId}/employee-count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartmentEmployeeCount(int departmentId)
        {
            _logger.LogInformation("Received request to get employee count for DepartmentId: {DepartmentId}", departmentId);
            try
            {
                var query = new GetDepartmentEmployeeCountQuery { DepartmentId = departmentId };
                var count = await _mediator.Send(query);
                _logger.LogInformation("Successfully retrieved employee count: {EmployeeCount} for DepartmentId: {DepartmentId}", count, departmentId);
                return Ok(new { DepartmentId = departmentId, EmployeeCount = count });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Department with ID: {DepartmentId} not found", departmentId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving employee count for DepartmentId: {DepartmentId}", departmentId);
                throw;
            }
        }

        [Authorize]
        [HttpGet("{departmentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartmentById(int departmentId)
        {
            _logger.LogInformation("Received request to get department by ID: {DepartmentId}", departmentId);
            try
            {
                var query = new GetDepartmentByIdQuery { DepartmentId = departmentId };
                var department = await _mediator.Send(query);
                var response = new
                {
                    DepartmentId = department.DepartmentId,
                    DepartmentName = department.DepartmentName,
                    Location = department.Location,
                    ManagerName = department.Manager != null ? $"{department.Manager.FirstName} {department.Manager.LastName}" : null
                };
                _logger.LogInformation("Successfully retrieved department with ID: {DepartmentId}", departmentId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Department with ID: {DepartmentId} not found", departmentId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving department with ID: {DepartmentId}", departmentId);
                throw;
            }
        }
    }
}