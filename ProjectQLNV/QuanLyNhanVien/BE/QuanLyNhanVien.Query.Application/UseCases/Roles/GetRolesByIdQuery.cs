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
    public class GetRolesByIdQuery : IRequest<Role>
    {
        public int RoleId { get; set; }
    }

    public class GetRolesByIdQueryValidator : AbstractValidator<GetRolesByIdQuery>
    {
        public GetRolesByIdQueryValidator()
        {
            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("RoleId phải lớn hơn 0.");
        }
    }

    public class GetRolesByIdQueryHandler : IRequestHandler<GetRolesByIdQuery, Role>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public GetRolesByIdQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Role> Handle(GetRolesByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetRolesByIdQuery for RoleId={RoleId}", request.RoleId);
            try
            {
                var repository = _unitOfWork.Repository<Role>();
                var role = await repository.GetAll()
                    .FirstOrDefaultAsync(r => r.RoleId == request.RoleId, cancellationToken);

                if (role == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found", request.RoleId);
                    throw new InvalidOperationException("Vai trò không tồn tại.");
                }

                _logger.LogInformation("Successfully retrieved role with ID {RoleId}", request.RoleId);
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetRolesByIdQuery for RoleId={RoleId}", request.RoleId);
                throw;
            }
        }
    }
}
