using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanVien.Command.Application.UseCases.Contracts;
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
    public class ContractsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ContractsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractCommand command)
        {
            var result = await _mediator.Send(command, CancellationToken.None);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{contractId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateContract(int contractId, [FromBody] UpdateContractCommand command)
        {
            command.ContractId = contractId;
            var result = await _mediator.Send(command, CancellationToken.None);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Hợp đồng không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{contractId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteContract(int contractId)
        {
            var command = new DeleteContractCommand { ContractId = contractId };
            var result = await _mediator.Send(command, CancellationToken.None);

            return result.IsSuccess
                ? Ok(result)
                : result.Error.Message.Contains("Hợp đồng không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
        }
    }
}
