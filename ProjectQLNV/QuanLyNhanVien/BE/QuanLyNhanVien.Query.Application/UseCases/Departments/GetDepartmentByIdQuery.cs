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
    public class GetDepartmentByIdQuery : IRequest<Department>
    {
        public int DepartmentId { get; set; }
    }

    public class GetDepartmentByIdQueryValidator : AbstractValidator<GetDepartmentByIdQuery>
    {
        public GetDepartmentByIdQueryValidator()
        {
            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("DepartmentId must be greater than 0.");
        }
    }

    public class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, Department>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetDepartmentByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Department> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Department>();
            var department = await repository.GetAll()
                .Include(d => d.Manager)
                .FirstOrDefaultAsync(d => d.DepartmentId == request.DepartmentId, cancellationToken);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found.");
            }

            return department;
        }
    }
}
