using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(IMediator mediator, ILogger<ContractsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractCommand command)
        {
            _logger.LogInformation("Received CreateContract request for EmployeeId: {EmployeeId}", command.EmployeeId);

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created contract for EmployeeId: {EmployeeId} with ID: {ContractId}",
                    command.EmployeeId, result.Data?.ContractId);
                return Ok(result);
            }
            _logger.LogWarning("Failed to create contract for EmployeeId: {EmployeeId}, Error: {Error}",
                command.EmployeeId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpPut("{contractId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Contract>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateContract(int contractId, [FromBody] UpdateContractCommand command)
        {
            _logger.LogInformation("Received UpdateContract request for ContractId: {ContractId}", contractId);

            command.ContractId = contractId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated contract with ID: {ContractId}", contractId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Hợp đồng không tồn tại"))
            {
                _logger.LogWarning("Contract with ID {ContractId} not found", contractId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to update contract with ID: {ContractId}, Error: {Error}",
                contractId, result.Error.Message);
            return BadRequest(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{contractId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<bool>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteContract(int contractId)
        {
            _logger.LogInformation("Received DeleteContract request for ContractId: {ContractId}", contractId);

            var command = new DeleteContractCommand { ContractId = contractId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted contract with ID: {ContractId}", contractId);
                return Ok(result);
            }
            if (result.Error.Message.Contains("Hợp đồng không tồn tại"))
            {
                _logger.LogWarning("Contract with ID {ContractId} not found", contractId);
                return NotFound(result);
            }
            _logger.LogWarning("Failed to delete contract with ID: {ContractId}, Error: {Error}",
                contractId, result.Error.Message);
            return BadRequest(result);
        }
    }
}
