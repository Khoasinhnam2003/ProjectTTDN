using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        public GetEmployeesByDepartmentQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<EmployeeDTO>> Handle(GetEmployeesByDepartmentQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Employee>();
            return await repository.GetAll()
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
                    PositionId = e.PositionId
                })
                .ToListAsync(cancellationToken);
        }
    }
}
