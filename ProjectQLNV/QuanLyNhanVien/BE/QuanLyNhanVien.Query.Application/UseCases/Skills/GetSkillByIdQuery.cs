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
        private readonly ILogger _logger;

        public GetSkillByIdQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Skill> Handle(GetSkillByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetSkillByIdQuery for SkillId={SkillId}", request.SkillId);
            try
            {
                var repository = _unitOfWork.Repository<Skill>();
                var skill = await repository.GetAll()
                    .Include(s => s.Employee)
                    .FirstOrDefaultAsync(s => s.SkillId == request.SkillId, cancellationToken);

                if (skill == null)
                {
                    _logger.LogWarning("Skill with ID {SkillId} not found", request.SkillId);
                    throw new InvalidOperationException("Skill with the given ID does not exist.");
                }

                _logger.LogInformation("Successfully retrieved skill with ID {SkillId}", request.SkillId);
                return skill;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetSkillByIdQuery for SkillId={SkillId}", request.SkillId);
                throw;
            }
        }
    }
}
