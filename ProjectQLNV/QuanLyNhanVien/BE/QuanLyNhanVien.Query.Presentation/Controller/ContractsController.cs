using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Application.UseCases.Contracts;
using QuanLyNhanVien.Query.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Presentation.Controller
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
        [HttpGet("{contractId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Contract))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetContractById(int contractId)
        {
            _logger.LogInformation("Received request to get contract by ID: {ContractId}", contractId);
            var query = new GetContractByIdQuery { ContractId = contractId };
            try
            {
                var result = await _mediator.Send(query, CancellationToken.None);
                _logger.LogInformation("Successfully retrieved contract with ID: {ContractId}", contractId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Contract with ID: {ContractId} not found", contractId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving contract with ID: {ContractId}", contractId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Contract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetContractsByEmployeeId(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Received request to get contracts for EmployeeId: {EmployeeId}, PageNumber: {PageNumber}, PageSize: {PageSize}", employeeId, pageNumber, pageSize);
            var query = new GetContractsByEmployeeIdQuery
            {
                EmployeeId = employeeId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            try
            {
                var result = await _mediator.Send(query, CancellationToken.None);
                _logger.LogInformation("Successfully retrieved {ContractCount} contracts for EmployeeId: {EmployeeId}", result.Count, employeeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving contracts for EmployeeId: {EmployeeId}", employeeId);
                throw;
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Contract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllContracts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Received request to get all contracts, PageNumber: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);
            var query = new GetAllContractsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            try
            {
                var result = await _mediator.Send(query, CancellationToken.None);
                _logger.LogInformation("Successfully retrieved {ContractCount} contracts", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving all contracts");
                throw;
            }
        }
    }
}
