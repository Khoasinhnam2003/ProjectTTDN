using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Application.UseCases.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SkillController> _logger;

        public SkillController(IMediator mediator, ILogger<SkillController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllSkills([FromQuery] GetAllSkillsQuery query)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested all skills with PageNumber={PageNumber} and PageSize={PageSize}", userId, query.PageNumber, query.PageSize);
            try
            {
                var skills = await _mediator.Send(query);
                var response = skills.Select(s => new
                {
                    SkillId = s.SkillId,
                    EmployeeId = s.EmployeeId,
                    EmployeeName = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : null,
                    SkillName = s.SkillName,
                    ProficiencyLevel = s.ProficiencyLevel
                }).ToList();
                _logger.LogInformation("Successfully returned {Count} skills for user {UserId}", response.Count, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching all skills for user {UserId}", userId);
                throw;
            }
        }

        [HttpGet("{skillId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSkillById(int skillId)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested skill with ID {SkillId}", userId, skillId);
            try
            {
                var query = new GetSkillByIdQuery { SkillId = skillId };
                var skill = await _mediator.Send(query);
                var response = new
                {
                    SkillId = skill.SkillId,
                    EmployeeId = skill.EmployeeId,
                    EmployeeName = skill.Employee != null ? $"{skill.Employee.FirstName} {skill.Employee.LastName}" : null,
                    SkillName = skill.SkillName,
                    ProficiencyLevel = skill.ProficiencyLevel,
                    Description = skill.Description,
                    CreatedAt = skill.CreatedAt,
                    UpdatedAt = skill.UpdatedAt
                };
                _logger.LogInformation("Successfully returned skill with ID {SkillId} for user {UserId}", skillId, userId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Skill with ID {SkillId} not found for user {UserId}", skillId, userId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching skill with ID {SkillId} for user {UserId}", skillId, userId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("by-employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSkillsByEmployee(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested skills for EmployeeId={EmployeeId} with PageNumber={PageNumber} and PageSize={PageSize}", userId, employeeId, pageNumber, pageSize);
            try
            {
                var query = new GetSkillsByEmployeeQuery
                {
                    EmployeeId = employeeId,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var skills = await _mediator.Send(query);
                var response = skills.Select(s => new
                {
                    SkillId = s.SkillId,
                    EmployeeId = s.EmployeeId,
                    EmployeeName = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : null,
                    SkillName = s.SkillName,
                    ProficiencyLevel = s.ProficiencyLevel,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList();
                if (skills.Any())
                {
                    _logger.LogInformation("Successfully returned {Count} skills for EmployeeId={EmployeeId} for user {UserId}", response.Count, employeeId, userId);
                    return Ok(response);
                }
                _logger.LogWarning("No skills found for EmployeeId={EmployeeId} for user {UserId}", employeeId, userId);
                return NotFound(new { Message = "No skills found for this employee." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching skills for EmployeeId={EmployeeId} for user {UserId}", employeeId, userId);
                throw;
            }
        }
    }
}
