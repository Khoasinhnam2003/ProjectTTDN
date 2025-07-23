using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public ContractsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("{contractId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Contract))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetContractById(int contractId)
        {
            var query = new GetContractByIdQuery { ContractId = contractId };
            try
            {
                var result = await _mediator.Send(query, CancellationToken.None);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
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
            var query = new GetContractsByEmployeeIdQuery
            {
                EmployeeId = employeeId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query, CancellationToken.None);
            return Ok(result);
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Contract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllContracts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetAllContractsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query, CancellationToken.None);
            return Ok(result);
        }
    }
}
