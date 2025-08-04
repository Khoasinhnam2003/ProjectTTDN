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

namespace QuanLyNhanVien.Query.Application.UseCases.SalaryHistories
{
    public class GetAllSalaryHistoriesQuery : IRequest<List<SalaryHistory>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllSalaryHistoriesQueryValidator : AbstractValidator<GetAllSalaryHistoriesQuery>
    {
        public GetAllSalaryHistoriesQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetAllSalaryHistoriesQueryHandler : IRequestHandler<GetAllSalaryHistoriesQuery, List<SalaryHistory>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllSalaryHistoriesQueryHandler> _logger;

        public GetAllSalaryHistoriesQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAllSalaryHistoriesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<SalaryHistory>> Handle(GetAllSalaryHistoriesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetAllSalaryHistoriesQuery with PageNumber={PageNumber}, PageSize={PageSize}", request.PageNumber, request.PageSize);
            try
            {
                var repository = _unitOfWork.Repository<SalaryHistory>();
                var salaryHistories = await repository.GetAll()
                    .Include(sh => sh.Employee)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
                _logger.LogInformation("Retrieved {Count} salary histories", salaryHistories.Count);
                return salaryHistories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetAllSalaryHistoriesQuery");
                throw;
            }
        }
    }
}
