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
    public class GetUserByIdQuery : IRequest<User>
    {
        public int UserId { get; set; }
    }

    public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdQueryValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.");
        }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, User>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<User> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<User>();
            var user = await repository.GetAll()
                .Include(u => u.Employee)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            return user;
        }
    }
}
