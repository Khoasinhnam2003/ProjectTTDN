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
    public class SearchEmployeesByNameQuery : IRequest<List<EmployeeDTO>>
    {
        public string Name { get; set; }
    }

    public class SearchEmployeesByNameQueryHandler : IRequestHandler<SearchEmployeesByNameQuery, List<EmployeeDTO>>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public SearchEmployeesByNameQueryHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<List<EmployeeDTO>> Handle(SearchEmployeesByNameQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Name))
                throw new ArgumentException("Tên không được để trống");

            var employees = await _employeeRepository.SearchByNameAsync(request.Name);
            return employees
                .Select(e => new EmployeeDTO
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Phone = e.Phone,
                    Salary = e.Salary,
                    Jobs = e.Jobs.Select(j => new JobDTO
                    {
                        Id = j.Id,
                        Title = j.Title,
                        Description = j.Description,
                        EmployeeId = j.EmployeeId
                    }).ToList()
                })
                .ToList();
        }
    }
}
