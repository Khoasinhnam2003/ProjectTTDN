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
        private readonly ILogger _logger;

        public GetUserByIdQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetUserByIdQuery for UserId={UserId}", request.UserId);
            try
            {
                var repository = _unitOfWork.Repository<User>();
                var user = await repository.GetAll()
                    .Include(u => u.Employee)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", request.UserId);
                    throw new InvalidOperationException("User not found.");
                }

                _logger.LogInformation("Successfully retrieved user with ID {UserId}", request.UserId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetUserByIdQuery for UserId={UserId}", request.UserId);
                throw;
            }
        }
    }
}
