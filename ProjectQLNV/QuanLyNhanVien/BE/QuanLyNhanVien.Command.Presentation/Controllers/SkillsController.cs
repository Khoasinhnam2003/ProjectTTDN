using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public SkillsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateSkill([FromBody] CreateSkillCommand command)
        {
            var result = await _mediator.Send(command, CancellationToken.None);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{skillId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Skill>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateSkill(int skillId, [FromBody] UpdateSkillCommand command)
        {
            command.SkillId = skillId;
            var result = await _mediator.Send(command, CancellationToken.None);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Kỹ năng không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{skillId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            var command = new DeleteSkillCommand { SkillId = skillId };
            var result = await _mediator.Send(command, CancellationToken.None);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Kỹ năng không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
        }
    }
}
