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

namespace QuanLyNhanVien.Query.Application.UseCases.Employees
{
    public class GetEmployeeByIdQuery : IRequest<Employee>
    {
        public int EmployeeId { get; set; }
    }

    public class GetEmployeeByIdQueryValidator : AbstractValidator<GetEmployeeByIdQuery>
    {
        public GetEmployeeByIdQueryValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId must be greater than 0.");
        }
    }

    public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Employee>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEmployeeByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Employee> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Employee>();
            return await repository.GetAll()
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.Attendances)
                .Include(e => e.Contracts)
                .Include(e => e.SalaryHistories)
                .Include(e => e.Skills)
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, cancellationToken)
                ?? throw new InvalidOperationException("Employee not found.");
        }
    }
}
