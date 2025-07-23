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

        public GetRolesByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Role> Handle(GetRolesByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Role>();
            var role = await repository.GetAll()
                .FirstOrDefaultAsync(r => r.RoleId == request.RoleId, cancellationToken);

            if (role == null)
            {
                throw new InvalidOperationException("Vai trò không tồn tại.");
            }

            return role;
        }
    }
}
