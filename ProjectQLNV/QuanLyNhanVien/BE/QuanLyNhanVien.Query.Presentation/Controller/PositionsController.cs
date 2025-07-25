﻿using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public PositionsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPositions([FromQuery] GetAllPositionsQuery query)
        {
            var positions = await _mediator.Send(query);
            var response = positions.Select(p => new
            {
                PositionId = p.PositionId,
                PositionName = p.PositionName,
                Description = p.Description,
                BaseSalary = p.BaseSalary
            }).ToList();
            return Ok(response);
        }

        [Authorize]
        [HttpGet("{positionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPositionById(int positionId)
        {
            try
            {
                var query = new GetPositionByIdQuery { PositionId = positionId };
                var position = await _mediator.Send(query);
                return Ok(new
                {
                    PositionId = position.PositionId,
                    PositionName = position.PositionName,
                    Description = position.Description,
                    BaseSalary = position.BaseSalary
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
