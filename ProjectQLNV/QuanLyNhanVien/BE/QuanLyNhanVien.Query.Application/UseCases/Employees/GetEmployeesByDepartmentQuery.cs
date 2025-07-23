using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Contracts.DTOs;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.Employees
{
    public class GetEmployeesByDepartmentQuery : IRequest<List<EmployeeDTO>>
    {
        public int DepartmentId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetEmployeesByDepartmentQueryValidator : AbstractValidator<GetEmployeesByDepartmentQuery>
    {
        public GetEmployeesByDepartmentQueryValidator()
        {
            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("DepartmentId must be greater than 0.");
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetEmployeesByDepartmentQueryHandler : IRequestHandler<GetEmployeesByDepartmentQuery, List<EmployeeDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetEmployeesByDepartmentQueryHandler> _logger;

        public GetEmployeesByDepartmentQueryHandler(IUnitOfWork unitOfWork, ILogger<GetEmployeesByDepartmentQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<EmployeeDTO>> Handle(GetEmployeesByDepartmentQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching employees for DepartmentId={DepartmentId} with PageNumber={PageNumber} and PageSize={PageSize}", request.DepartmentId, request.PageNumber, request.PageSize);
            var repository = _unitOfWork.Repository<Employee>();
            var employees = await repository.GetAll()
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.DepartmentId == request.DepartmentId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new EmployeeDTO
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Email = e.Email,
                    Phone = e.Phone,
                    DepartmentId = e.DepartmentId,
                    PositionId = e.PositionId,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : null,
                    PositionName = e.Position != null ? e.Position.PositionName : null
                })
                .ToListAsync(cancellationToken);
            _logger.LogInformation("Successfully fetched {Count} employees for DepartmentId={DepartmentId}", employees.Count, request.DepartmentId);
            return employees;
        }
    }
}
