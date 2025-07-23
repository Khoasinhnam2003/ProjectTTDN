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

namespace QuanLyNhanVien.Query.Application.UseCases.UserRoles
{
    public class GetAllUserRolesQuery : IRequest<List<UserRole>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllUserRolesQueryValidator : AbstractValidator<GetAllUserRolesQuery>
    {
        public GetAllUserRolesQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize không được vượt quá 100.");
        }
    }

    public class GetAllUserRolesQueryHandler : IRequestHandler<GetAllUserRolesQuery, List<UserRole>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllUserRolesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<UserRole>> Handle(GetAllUserRolesQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<UserRole>();
            return await repository.GetAll()
                .Include(ur => ur.User)
                    .ThenInclude(u => u.Employee)
                .Include(ur => ur.Role)
                .OrderBy(ur => ur.UserId)
                .ThenBy(ur => ur.RoleId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
