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
    public class GetAllSkillsQuery : IRequest<List<Skill>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllSkillsQueryValidator : AbstractValidator<GetAllSkillsQuery>
    {
        public GetAllSkillsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetAllSkillsQueryHandler : IRequestHandler<GetAllSkillsQuery, List<Skill>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public GetAllSkillsQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Skill>> Handle(GetAllSkillsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetAllSkillsQuery with PageNumber={PageNumber}, PageSize={PageSize}", request.PageNumber, request.PageSize);
            try
            {
                var repository = _unitOfWork.Repository<Skill>();
                var skills = await repository.GetAll()
                    .Include(s => s.Employee)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
                _logger.LogInformation("Retrieved {Count} skills", skills.Count);
                return skills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetAllSkillsQuery");
                throw;
            }
        }
    }
}
