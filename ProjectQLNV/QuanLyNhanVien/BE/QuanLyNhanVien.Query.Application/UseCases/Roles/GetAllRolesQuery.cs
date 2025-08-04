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

namespace QuanLyNhanVien.Query.Application.UseCases.Roles
{
    public class GetAllRolesQuery : IRequest<List<Role>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllRolesQueryValidator : AbstractValidator<GetAllRolesQuery>
    {
        public GetAllRolesQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize không được vượt quá 100.");
        }
    }

    public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, List<Role>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllRolesQueryHandler> _logger;

        public GetAllRolesQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAllRolesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Role>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetAllRolesQuery with PageNumber={PageNumber}, PageSize={PageSize}", request.PageNumber, request.PageSize);
            try
            {
                var repository = _unitOfWork.Repository<Role>();
                var roles = await repository.GetAll()
                    .OrderBy(r => r.RoleId)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
                _logger.LogInformation("Retrieved {Count} roles", roles.Count);
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetAllRolesQuery");
                throw;
            }
        }
    }
}
