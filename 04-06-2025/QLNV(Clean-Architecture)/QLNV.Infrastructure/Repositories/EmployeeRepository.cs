using Microsoft.EntityFrameworkCore;
using QLNV.Domain;
using QLNV.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EmployeeRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            return await _dbContext.Employees
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<Employee> GetByIdAsync(int id)
        {
            return await _dbContext.Employees.FindAsync(id);
        }

        public async Task<Employee> AddAsync(Employee employee)
        {
            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee> UpdateAsync(Employee employee)
        {
            var existingEmployee = await _dbContext.Employees.FindAsync(employee.Id);
            if (existingEmployee == null) return null;

            existingEmployee.Name = employee.Name;
            existingEmployee.Email = employee.Email;
            existingEmployee.Phone = employee.Phone;
            existingEmployee.Salary = employee.Salary;

            await _dbContext.SaveChangesAsync();
            return existingEmployee;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var employee = await _dbContext.Employees.FindAsync(id);
            if (employee == null) return false;

            _dbContext.Employees.Remove(employee);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<Employee>> GetBySalaryAsync(decimal minSalary)
        {
            return await _dbContext.Employees
                .Where(e => e.Salary >= minSalary)
                .OrderBy(e => e.Salary)
                .ToListAsync();
        }

        public async Task<List<Employee>> SearchByNameAsync(string name)
        {
            return await _dbContext.Employees
                .Where(e => e.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync();
        }
    }
}
