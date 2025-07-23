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

namespace QuanLyNhanVien.Query.Application.UseCases.Employees
{
    public class GetEmployeesByIdQuery : IRequest<Employee>
    {
        public int EmployeeId { get; set; }
    }

    public class GetEmployeeByIdQueryValidator : AbstractValidator<GetEmployeesByIdQuery>
    {
        public GetEmployeeByIdQueryValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId must be greater than 0.");
        }
    }

    public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeesByIdQuery, Employee>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetEmployeeByIdQueryHandler> _logger;

        public GetEmployeeByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetEmployeeByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Employee> Handle(GetEmployeesByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching employee with ID {EmployeeId}", request.EmployeeId);
            var repository = _unitOfWork.Repository<Employee>();
            var employee = await repository.GetAll()
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.Attendances)
                .Include(e => e.Contracts)
                .Include(e => e.SalaryHistories)
                .Include(e => e.Skills)
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, cancellationToken)
                ?? throw new InvalidOperationException("Employee not found.");
            _logger.LogInformation("Successfully fetched employee with ID {EmployeeId}", request.EmployeeId);
            return employee;
        }
    }
}
