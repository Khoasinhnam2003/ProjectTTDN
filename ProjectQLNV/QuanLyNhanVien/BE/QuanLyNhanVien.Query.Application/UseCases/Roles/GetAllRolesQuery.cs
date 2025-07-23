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

        public GetAllRolesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<Role>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Role>();
            return await repository.GetAll()
                .OrderBy(r => r.RoleId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
