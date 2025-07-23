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
    public class GetAllContractsQuery : IRequest<List<Contract>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllContractsQueryValidator : AbstractValidator<GetAllContractsQuery>
    {
        public GetAllContractsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize không được vượt quá 100.");
        }
    }

    public class GetAllContractsQueryHandler : IRequestHandler<GetAllContractsQuery, List<Contract>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllContractsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<Contract>> Handle(GetAllContractsQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Contract>();
            return await repository.GetAll()
                .Include(c => c.Employee)
                .OrderBy(c => c.StartDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
