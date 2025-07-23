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

        public GetSalaryHistoryByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<SalaryHistory> Handle(GetSalaryHistoryByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<SalaryHistory>();
            var salaryHistory = await repository.GetAll()
                .Include(sh => sh.Employee)
                .FirstOrDefaultAsync(sh => sh.SalaryHistoryId == request.SalaryHistoryId, cancellationToken);
            if (salaryHistory == null)
            {
                throw new InvalidOperationException("Lịch sử lương không tồn tại.");
            }
            return salaryHistory;
        }
    }
}
