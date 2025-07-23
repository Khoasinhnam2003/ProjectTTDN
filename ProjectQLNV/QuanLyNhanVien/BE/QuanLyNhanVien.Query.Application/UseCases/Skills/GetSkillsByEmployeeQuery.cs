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

namespace QuanLyNhanVien.Query.Application.UseCases.Skills
{
    public class GetSkillsByEmployeeQuery : IRequest<List<Skill>>
    {
        public int EmployeeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetSkillsByEmployeeQueryValidator : AbstractValidator<GetSkillsByEmployeeQuery>
    {
        public GetSkillsByEmployeeQueryValidator()
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

    public class GetSkillsByEmployeeQueryHandler : IRequestHandler<GetSkillsByEmployeeQuery, List<Skill>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSkillsByEmployeeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<Skill>> Handle(GetSkillsByEmployeeQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Skill>();
            return await repository.GetAll()
                .Include(s => s.Employee)
                .Where(s => s.EmployeeId == request.EmployeeId)
                .OrderBy(s => s.SkillId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
