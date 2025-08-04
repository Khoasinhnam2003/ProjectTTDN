using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Application.UseCases.Positions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(IMediator mediator, ILogger<PositionsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPositions([FromQuery] GetAllPositionsQuery query)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested all positions with PageNumber={PageNumber} and PageSize={PageSize}", userId, query.PageNumber, query.PageSize);
            try
            {
                var positions = await _mediator.Send(query);
                var response = positions.Select(p => new
                {
                    PositionId = p.PositionId,
                    PositionName = p.PositionName,
                    Description = p.Description,
                    BaseSalary = p.BaseSalary
                }).ToList();
                _logger.LogInformation("Successfully returned {Count} positions for user {UserId}", response.Count, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching all positions for user {UserId}", userId);
                throw;
            }
        }

        [Authorize]
        [HttpGet("{positionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPositionById(int positionId)
        {
            var userId = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} requested position with ID {PositionId}", userId, positionId);
            try
            {
                var query = new GetPositionByIdQuery { PositionId = positionId };
                var position = await _mediator.Send(query);
                var response = new
                {
                    PositionId = position.PositionId,
                    PositionName = position.PositionName,
                    Description = position.Description,
                    BaseSalary = position.BaseSalary
                };
                _logger.LogInformation("Successfully returned position with ID {PositionId} for user {UserId}", positionId, userId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Position with ID {PositionId} not found for user {UserId}", positionId, userId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching position with ID {PositionId} for user {UserId}", positionId, userId);
                throw;
            }
        }
    }
}
