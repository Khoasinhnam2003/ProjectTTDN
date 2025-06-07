using QLNV.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLNV.Infrastructure.Repositories
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();
        Task<Employee> GetByIdAsync(int id);
        Task<Employee> AddAsync(Employee employee);
        Task<Employee> UpdateAsync(Employee employee);
        Task<bool> DeleteAsync(int id);
        Task<List<Employee>> GetBySalaryAsync(decimal minSalary);
        Task<List<Employee>> SearchByNameAsync(string name);
        Task<Job> AddJobAsync(Job job);
        Task<List<Job>> GetJobsByEmployeeIdAsync(int employeeId);
        Task<Job> UpdateJobAsync(Job job);
        Task<bool> DeleteJobAsync(int jobId);
    }
}