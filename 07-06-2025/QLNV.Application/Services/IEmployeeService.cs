using QLNV.Application.DTOs;
using QLNV.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Application.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task<Employee> AddEmployeeAsync(AddEmployeeDto addEmployeeDto);
        Task<Employee> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<List<Employee>> GetEmployeesBySalaryAsync(decimal minSalary);
        Task<List<Employee>> SearchEmployeesByNameAsync(string name);
        Task<Job> AddJobAsync(int employeeId, AddJobDto addJobDto);
        Task<List<Job>> GetJobsByEmployeeIdAsync(int employeeId);
        Task<Job> UpdateJobAsync(int jobId, UpdateJobDto updateJobDto);
        Task<bool> DeleteJobAsync(int jobId);
    }
}
