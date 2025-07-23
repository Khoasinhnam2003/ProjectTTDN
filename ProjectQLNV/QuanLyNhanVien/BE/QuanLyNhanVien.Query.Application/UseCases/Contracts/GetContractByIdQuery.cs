using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        public GetContractByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Contract> Handle(GetContractByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Contract>();
            var contract = await repository.GetAll()
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.ContractId == request.ContractId, cancellationToken);

            if (contract == null)
            {
                throw new InvalidOperationException("Hợp đồng không tồn tại.");
            }
            return contract;
        }
    }
}
