using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Application.UseCases.Skills;
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
    public class SkillsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SkillsController> _logger;

        public SkillsController(IMediator mediator, ILogger<SkillsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateSkill([FromBody] CreateSkillCommand command)
        {
            _logger.LogInformation("Received CreateSkill request for employee ID: {EmployeeId}, SkillName: {SkillName}",
                command.EmployeeId, command.SkillName);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created skill with ID: {SkillId} for employee ID: {EmployeeId}",
                    result.Data.SkillId, command.EmployeeId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to create skill for employee ID {EmployeeId}, SkillName: {SkillName}, Error: {Error}",
                command.EmployeeId, command.SkillName, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPut("{skillId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateSkill(int skillId, [FromBody] UpdateSkillCommand command)
        {
            _logger.LogInformation("Received UpdateSkill request for skill ID: {SkillId}", skillId);

            if (skillId != command.SkillId)
            {
                _logger.LogWarning("SkillId mismatch: URL SkillId {UrlSkillId} does not match body SkillId {BodySkillId}",
                    skillId, command.SkillId);
                return BadRequest(new { Message = "SkillId trong URL không khớp với command." });
            }

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated skill with ID: {SkillId}", skillId);
                return Ok(result);
            }

            if (result.Error.Message.Contains("Kỹ năng không tồn tại"))
            {
                _logger.LogWarning("Skill with ID {SkillId} not found", skillId);
                return NotFound(result);
            }

            _logger.LogWarning("Failed to update skill with ID {SkillId}, Error: {Error}", skillId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{skillId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            _logger.LogInformation("Received DeleteSkill request for skill ID: {SkillId}", skillId);

            var command = new DeleteSkillCommand { SkillId = skillId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted skill with ID: {SkillId}", skillId);
                return Ok(result);
            }

            if (result.Error.Message.Contains("Kỹ năng không tồn tại"))
            {
                _logger.LogWarning("Skill with ID {SkillId} not found", skillId);
                return NotFound(result);
            }

            _logger.LogWarning("Failed to delete skill with ID {SkillId}, Error: {Error}", skillId, result.Error.Message);
            return BadRequest(result);
        }
    }
}
