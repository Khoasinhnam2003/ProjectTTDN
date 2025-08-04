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
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.Departments
{
    public class GetAllDepartmentsQuery : IRequest<List<DepartmentDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllDepartmentsQueryValidator : AbstractValidator<GetAllDepartmentsQuery>
    {
        public GetAllDepartmentsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetAllDepartmentsQueryHandler : IRequestHandler<GetAllDepartmentsQuery, List<DepartmentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public GetAllDepartmentsQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<DepartmentDto>> Handle(GetAllDepartmentsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetAllDepartmentsQuery, PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.PageNumber, request.PageSize);
            try
            {
                var departmentRepository = _unitOfWork.Repository<Department>();
                var employeeRepository = _unitOfWork.Repository<Employee>();

                var departments = await departmentRepository.GetAll()
                    .Include(d => d.Manager)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                var departmentDtos = departments.Select(d => new DepartmentDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    Location = d.Location,
                    ManagerName = d.Manager != null ? $"{d.Manager.FirstName} {d.Manager.LastName}" : null,
                    EmployeeCount = employeeRepository.GetAll().Count(e => e.DepartmentId == d.DepartmentId)
                }).ToList();

                _logger.LogInformation("Retrieved {DepartmentCount} departments", departmentDtos.Count);
                return departmentDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetAllDepartmentsQuery");
                throw;
            }
        }
    }
}