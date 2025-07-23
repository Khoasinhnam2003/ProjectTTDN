using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public SkillController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllSkills([FromQuery] GetAllSkillsQuery query)
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
            return Ok(response);
        }

        [HttpGet("{skillId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSkillById(int skillId)
        {
            try
            {
                var query = new GetSkillByIdQuery { SkillId = skillId };
                var skill = await _mediator.Send(query);
                return Ok(new
                {
                    SkillId = skill.SkillId,
                    EmployeeId = skill.EmployeeId,
                    EmployeeName = skill.Employee != null ? $"{skill.Employee.FirstName} {skill.Employee.LastName}" : null,
                    SkillName = skill.SkillName,
                    ProficiencyLevel = skill.ProficiencyLevel,
                    Description = skill.Description,
                    CreatedAt = skill.CreatedAt,
                    UpdatedAt = skill.UpdatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("by-employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSkillsByEmployee(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
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
            return skills.Any() ? Ok(response) : NotFound(new { Message = "No skills found for this employee." });
        }
    }
}
