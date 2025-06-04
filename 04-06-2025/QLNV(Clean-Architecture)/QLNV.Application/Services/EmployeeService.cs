using QLNV.Application.DTOs;
using QLNV.Domain;
using QLNV.Infrastructure.Repositories;

namespace QLNV.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _employeeRepository.GetAllAsync();
        }

        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            return await _employeeRepository.GetByIdAsync(id);
        }

        public async Task<Employee> AddEmployeeAsync(AddEmployeeDto addEmployeeDto)
        {
            var employee = new Employee
            {
                Name = addEmployeeDto.Name,
                Email = addEmployeeDto.Email,
                Phone = addEmployeeDto.Phone,
                Salary = addEmployeeDto.Salary
            };
            return await _employeeRepository.AddAsync(employee);
        }

        public async Task<Employee> UpdateEmployeeAsync(UpdateEmployeeDto updateEmployeeDto)
        {
            var employee = new Employee
            {
                Id = updateEmployeeDto.Id,
                Name = updateEmployeeDto.Name,
                Email = updateEmployeeDto.Email,
                Phone = updateEmployeeDto.Phone,
                Salary = updateEmployeeDto.Salary
            };

            return await _employeeRepository.UpdateAsync(employee);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            return await _employeeRepository.DeleteAsync(id);
        }

        public async Task<List<Employee>> GetEmployeesBySalaryAsync(decimal minSalary)
        {
            if (minSalary < 0)
                throw new ArgumentException("Minimum salary cannot be negative");

            return await _employeeRepository.GetBySalaryAsync(minSalary);
        }

        public async Task<List<Employee>> SearchEmployeesByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is required");

            return await _employeeRepository.SearchByNameAsync(name);
        }
    }
}