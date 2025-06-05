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

            var employees = await _employeeRepository.GetAllAsync();
            return employees
                .Where(e => e.Name.ToLower().Contains(request.Name.ToLower()))
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
