using QLNV.Models.Entities;

namespace QLNV.Repositories
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();
        Task<Employee> GetByIdAsync(int id);
        Task<Employee> AddAsync(Employee employee);
        Task<Employee> UpdateAsync(int id, Employee employee);
        Task<bool> DeleteAsync(int id);
        Task<List<Employee>> GetBySalaryAsync(decimal minSalary);
        Task<List<Employee>> SearchByNameAsync(string name);
    }
}
