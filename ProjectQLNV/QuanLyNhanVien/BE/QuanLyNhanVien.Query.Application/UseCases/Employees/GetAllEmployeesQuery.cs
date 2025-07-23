using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using QuanLyNhanVien.Query.Persistence;

namespace QuanLyNhanVien.Query.Application.UseCases.Employees
{
    public class GetAllEmployeesQuery : IRequest<List<Employee>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllEmployeesQueryValidator : AbstractValidator<GetAllEmployeesQuery>
    {
        public GetAllEmployeesQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, List<Employee>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllEmployeesQueryHandler> _logger;

        public GetAllEmployeesQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAllEmployeesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Employee>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all employees with PageNumber={PageNumber} and PageSize={PageSize}", request.PageNumber, request.PageSize);
            var repository = _unitOfWork.Repository<Employee>();
            var employees = await repository.GetAll()
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
            _logger.LogInformation("Successfully fetched {Count} employees", employees.Count);
            return employees;
        }
    }
}
