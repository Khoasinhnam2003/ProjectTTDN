using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanVien.Query.Contracts.DTOs;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.SalaryHistories
{
    public class GetSalaryHistoriesByEmployeeQuery : IRequest<List<SalaryHistoryDTO>>
    {
        public int EmployeeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetSalaryHistoriesByEmployeeQueryValidator : AbstractValidator<GetSalaryHistoriesByEmployeeQuery>
    {
        public GetSalaryHistoriesByEmployeeQueryValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId must be greater than 0.");
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetSalaryHistoriesByEmployeeQueryHandler : IRequestHandler<GetSalaryHistoriesByEmployeeQuery, List<SalaryHistoryDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSalaryHistoriesByEmployeeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<SalaryHistoryDTO>> Handle(GetSalaryHistoriesByEmployeeQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<SalaryHistory>();
            return await repository.GetAll()
                .Include(sh => sh.Employee)
                .Where(sh => sh.EmployeeId == request.EmployeeId)
                .OrderBy(sh => sh.EffectiveDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(sh => new SalaryHistoryDTO
                {
                    SalaryHistoryId = sh.SalaryHistoryId,
                    EmployeeId = sh.EmployeeId,
                    EmployeeName = sh.Employee != null ? $"{sh.Employee.FirstName} {sh.Employee.LastName}" : null,
                    Salary = sh.Salary,
                    EffectiveDate = sh.EffectiveDate,
                    CreatedAt = sh.CreatedAt,
                    UpdatedAt = sh.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
