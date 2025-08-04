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
    public class GetContractsByEmployeeIdQuery : IRequest<List<Contract>>
    {
        public int EmployeeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetContractsByEmployeeIdQueryValidator : AbstractValidator<GetContractsByEmployeeIdQuery>
    {
        public GetContractsByEmployeeIdQueryValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId phải lớn hơn 0.");
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize không được vượt quá 100.");
        }
    }

    public class GetContractsByEmployeeIdQueryHandler : IRequestHandler<GetContractsByEmployeeIdQuery, List<Contract>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetContractsByEmployeeIdQueryHandler> _logger;

        public GetContractsByEmployeeIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetContractsByEmployeeIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Contract>> Handle(GetContractsByEmployeeIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetContractsByEmployeeIdQuery for EmployeeId: {EmployeeId}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.EmployeeId, request.PageNumber, request.PageSize);

            try
            {
                var repository = _unitOfWork.Repository<Contract>();
                var contracts = await repository.GetAll()
                    .Include(c => c.Employee)
                    .Where(c => c.EmployeeId == request.EmployeeId)
                    .OrderBy(c => c.StartDate)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {ContractCount} contracts for EmployeeId: {EmployeeId}", contracts.Count, request.EmployeeId);
                return contracts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetContractsByEmployeeIdQuery for EmployeeId: {EmployeeId}", request.EmployeeId);
                throw;
            }
        }
    }
}
