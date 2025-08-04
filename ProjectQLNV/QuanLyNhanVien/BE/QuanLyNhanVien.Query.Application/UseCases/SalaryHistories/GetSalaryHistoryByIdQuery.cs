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
    public class GetSalaryHistoryByIdQuery : IRequest<SalaryHistory>
    {
        public int SalaryHistoryId { get; set; }
    }

    public class GetSalaryHistoryByIdQueryValidator : AbstractValidator<GetSalaryHistoryByIdQuery>
    {
        public GetSalaryHistoryByIdQueryValidator()
        {
            RuleFor(x => x.SalaryHistoryId)
                .GreaterThan(0).WithMessage("SalaryHistoryId phải lớn hơn 0.");
        }
    }

    public class GetSalaryHistoryByIdQueryHandler : IRequestHandler<GetSalaryHistoryByIdQuery, SalaryHistory>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetSalaryHistoryByIdQueryHandler> _logger;

        public GetSalaryHistoryByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetSalaryHistoryByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SalaryHistory> Handle(GetSalaryHistoryByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetSalaryHistoryByIdQuery for SalaryHistoryId={SalaryHistoryId}", request.SalaryHistoryId);
            try
            {
                var repository = _unitOfWork.Repository<SalaryHistory>();
                var salaryHistory = await repository.GetAll()
                    .Include(sh => sh.Employee)
                    .FirstOrDefaultAsync(sh => sh.SalaryHistoryId == request.SalaryHistoryId, cancellationToken);
                if (salaryHistory == null)
                {
                    _logger.LogWarning("Salary history with ID {SalaryHistoryId} not found", request.SalaryHistoryId);
                    throw new InvalidOperationException("Lịch sử lương không tồn tại.");
                }
                _logger.LogInformation("Successfully retrieved salary history with ID {SalaryHistoryId}", request.SalaryHistoryId);
                return salaryHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetSalaryHistoryByIdQuery for SalaryHistoryId={SalaryHistoryId}", request.SalaryHistoryId);
                throw;
            }
        }
    }
}
