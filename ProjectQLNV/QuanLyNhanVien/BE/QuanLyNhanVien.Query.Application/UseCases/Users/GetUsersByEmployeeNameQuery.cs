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
    public class GetUsersByEmployeeNameQuery : IRequest<List<User>>
    {
        public string Name { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetUsersByEmployeeNameQueryValidator : AbstractValidator<GetUsersByEmployeeNameQuery>
    {
        public GetUsersByEmployeeNameQueryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên tìm kiếm không được để trống.")
                .MaximumLength(100).WithMessage("Tên tìm kiếm không được vượt quá 100 ký tự.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetUsersByEmployeeNameQueryHandler : IRequestHandler<GetUsersByEmployeeNameQuery, List<User>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public GetUsersByEmployeeNameQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<User>> Handle(GetUsersByEmployeeNameQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetUsersByEmployeeNameQuery with Name={Name}, PageNumber={PageNumber}, PageSize={PageSize}", request.Name, request.PageNumber, request.PageSize);
            try
            {
                var repository = _unitOfWork.Repository<User>();
                var query = repository.GetAll()
                    .Include(u => u.Employee)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Where(u => u.Employee != null && (
                        EF.Functions.Like(u.Employee.FirstName, $"%{request.Name}%") ||
                        EF.Functions.Like(u.Employee.LastName, $"%{request.Name}%") ||
                        EF.Functions.Like(
                            EF.Functions.Collate(
                                u.Employee.FirstName + " " + u.Employee.LastName,
                                "SQL_Latin1_General_CP1_CI_AS"),
                            $"%{request.Name}%")));

                var users = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
                _logger.LogInformation("Retrieved {Count} users for search Name={Name}", users.Count, request.Name);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetUsersByEmployeeNameQuery for Name={Name}", request.Name);
                throw;
            }
        }
    }
}
