using MediatR;
using QLNV.Application.DTOs;
using QLNV.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Application.Queries
{
    public class GetEmployeesBySalaryQuery : IRequest<List<EmployeeDTO>>
    {
        public decimal MinSalary { get; set; }
    }

    public class GetEmployeesBySalaryQueryHandler : IRequestHandler<GetEmployeesBySalaryQuery, List<EmployeeDTO>>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public GetEmployeesBySalaryQueryHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<List<EmployeeDTO>> Handle(GetEmployeesBySalaryQuery request, CancellationToken cancellationToken)
        {
            if (request.MinSalary < 0)
                throw new ArgumentException("Lương tối thiểu không thể âm");

            var employees = await _employeeRepository.GetAllAsync();
            return employees
                .Where(e => e.Salary >= request.MinSalary)
                .OrderBy(e => e.Salary)
                .Select(e => new EmployeeDTO
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Phone = e.Phone,
                    Salary = e.Salary
                })
                .ToList();
        }
    }
}
