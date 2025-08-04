using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Application.UseCases.Positions;
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
    public class PositionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(IMediator mediator, ILogger<PositionsController> logger)
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
        public async Task<IActionResult> CreatePosition([FromBody] CreatePositionCommand command)
        {
            _logger.LogInformation("Received CreatePosition request for position name: {PositionName}", command.PositionName);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created position with name: {PositionName}", command.PositionName);
                return Ok(result);
            }
            _logger.LogWarning("Failed to create position with name {PositionName}, Error: {Error}", command.PositionName, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{positionId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePosition(int positionId, [FromBody] UpdatePositionCommand command)
        {
            _logger.LogInformation("Received UpdatePosition request for position ID: {PositionId}", positionId);

            command.PositionId = positionId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated position with ID: {PositionId}", positionId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Vị trí không tồn tại"))
            {
                _logger.LogWarning("Position with ID {PositionId} not found", positionId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to update position with ID {PositionId}, Error: {Error}", positionId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{positionId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeletePosition(int positionId)
        {
            _logger.LogInformation("Received DeletePosition request for position ID: {PositionId}", positionId);

            var command = new DeletePositionCommand { PositionId = positionId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted position with ID: {PositionId}", positionId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Vị trí không tồn tại"))
            {
                _logger.LogWarning("Position with ID {PositionId} not found", positionId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to delete position with ID {PositionId}, Error: {Error}", positionId, result.Error.Message);
            return BadRequest(result);
        }
    }
}
