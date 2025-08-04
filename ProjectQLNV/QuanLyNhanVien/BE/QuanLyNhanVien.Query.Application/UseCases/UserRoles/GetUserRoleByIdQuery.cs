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

namespace QuanLyNhanVien.Query.Application.UseCases.UserRoles
{
    public class GetUserRoleByIdQuery : IRequest<UserRole>
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }

    public class GetUserRoleByIdQueryValidator : AbstractValidator<GetUserRoleByIdQuery>
    {
        public GetUserRoleByIdQueryValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId phải lớn hơn 0.");

            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("RoleId phải lớn hơn 0.");
        }
    }

    public class GetUserRoleByIdQueryHandler : IRequestHandler<GetUserRoleByIdQuery, UserRole>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public GetUserRoleByIdQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserRole> Handle(GetUserRoleByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetUserRoleByIdQuery for UserId={UserId}, RoleId={RoleId}", request.UserId, request.RoleId);
            try
            {
                var repository = _unitOfWork.Repository<UserRole>();
                var userRole = await repository.GetAll()
                    .Include(ur => ur.User)
                        .ThenInclude(u => u.Employee)
                    .Include(ur => ur.Role)
                    .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);

                if (userRole == null)
                {
                    _logger.LogWarning("User role with UserId={UserId} and RoleId={RoleId} not found", request.UserId, request.RoleId);
                    throw new InvalidOperationException("Liên kết người dùng-vai trò không tồn tại.");
                }

                _logger.LogInformation("Successfully retrieved user role with UserId={UserId} and RoleId={RoleId}", request.UserId, request.RoleId);
                return userRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetUserRoleByIdQuery for UserId={UserId}, RoleId={RoleId}", request.UserId, request.RoleId);
                throw;
            }
        }
    }
}
