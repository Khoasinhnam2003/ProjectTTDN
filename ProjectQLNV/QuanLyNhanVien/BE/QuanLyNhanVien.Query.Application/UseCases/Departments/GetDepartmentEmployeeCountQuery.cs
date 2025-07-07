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

namespace QuanLyNhanVien.Query.Application.UseCases.Departments
{
    public class GetDepartmentEmployeeCountQuery : IRequest<int>
    {
        public int DepartmentId { get; set; }
    }

    public class GetDepartmentEmployeeCountQueryValidator : AbstractValidator<GetDepartmentEmployeeCountQuery>
    {
        public GetDepartmentEmployeeCountQueryValidator()
        {
            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("DepartmentId must be greater than 0.");
        }
    }

    public class GetDepartmentEmployeeCountQueryHandler : IRequestHandler<GetDepartmentEmployeeCountQuery, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetDepartmentEmployeeCountQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<int> Handle(GetDepartmentEmployeeCountQuery request, CancellationToken cancellationToken)
        {
            var employeeRepository = _unitOfWork.Repository<Employee>();
            return await employeeRepository.GetAll()
                .CountAsync(e => e.DepartmentId == request.DepartmentId, cancellationToken);
        }
    }
}
