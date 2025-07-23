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

namespace QuanLyNhanVien.Query.Application.UseCases.Users
{
    public class GetAllUsersQuery : IRequest<List<User>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
    {
        public GetAllUsersQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<User>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllUsersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<User>();
            return await repository.GetAll()
                .Include(u => u.Employee)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
