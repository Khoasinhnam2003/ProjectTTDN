using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.Contracts
{
    public class GetContractByIdQuery : IRequest<Contract>
    {
        public int ContractId { get; set; }
    }

    public class GetContractByIdQueryValidator : AbstractValidator<GetContractByIdQuery>
    {
        public GetContractByIdQueryValidator()
        {
            RuleFor(x => x.ContractId)
                .GreaterThan(0).WithMessage("ContractId phải lớn hơn 0.");
        }
    }

    public class GetContractByIdQueryHandler : IRequestHandler<GetContractByIdQuery, Contract>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetContractByIdQueryHandler> _logger;

        public GetContractByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetContractByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Contract> Handle(GetContractByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetContractByIdQuery for ContractId: {ContractId}", request.ContractId);

            try
            {
                var repository = _unitOfWork.Repository<Contract>();
                var contract = await repository.GetAll()
                    .Include(c => c.Employee)
                    .FirstOrDefaultAsync(c => c.ContractId == request.ContractId, cancellationToken);

                if (contract == null)
                {
                    _logger.LogWarning("Contract with ID: {ContractId} not found", request.ContractId);
                    throw new InvalidOperationException("Hợp đồng không tồn tại.");
                }

                _logger.LogInformation("Successfully retrieved contract with ID: {ContractId}", request.ContractId);
                return contract;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetContractByIdQuery for ContractId: {ContractId}", request.ContractId);
                throw;
            }
        }
    }
}
