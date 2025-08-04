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
        private readonly ILogger _logger;

        public GetSkillsByEmployeeQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Skill>> Handle(GetSkillsByEmployeeQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetSkillsByEmployeeQuery for EmployeeId={EmployeeId}, PageNumber={PageNumber}, PageSize={PageSize}", request.EmployeeId, request.PageNumber, request.PageSize);
            try
            {
                var repository = _unitOfWork.Repository<Skill>();
                var skills = await repository.GetAll()
                    .Include(s => s.Employee)
                    .Where(s => s.EmployeeId == request.EmployeeId)
                    .OrderBy(s => s.SkillId)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
                _logger.LogInformation("Retrieved {Count} skills for EmployeeId={EmployeeId}", skills.Count, request.EmployeeId);
                return skills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetSkillsByEmployeeQuery for EmployeeId={EmployeeId}", request.EmployeeId);
                throw;
            }
        }
    }
}
