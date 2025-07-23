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
    public class GetSkillByIdQuery : IRequest<Skill>
    {
        public int SkillId { get; set; }
    }

    public class GetSkillByIdQueryValidator : AbstractValidator<GetSkillByIdQuery>
    {
        public GetSkillByIdQueryValidator()
        {
            RuleFor(x => x.SkillId)
                .GreaterThan(0).WithMessage("SkillId must be greater than 0.");
        }
    }

    public class GetSkillByIdQueryHandler : IRequestHandler<GetSkillByIdQuery, Skill>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSkillByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Skill> Handle(GetSkillByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Skill>();
            var skill = await repository.GetAll()
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.SkillId == request.SkillId, cancellationToken);

            if (skill == null)
            {
                throw new InvalidOperationException("Skill with the given ID does not exist.");
            }

            return skill;
        }
    }
}
