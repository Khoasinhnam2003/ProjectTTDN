using Microsoft.EntityFrameworkCore;
using QLNV.Domain;
using QLNV.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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
                .AsNoTracking()
                .Select(e => new Employee
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Phone = e.Phone,
                    Salary = e.Salary,
                    Jobs = e.Jobs.Select(j => new Job
                    {
                        Id = j.Id,
                        Title = j.Title,
                        Description = j.Description,
                        EmployeeId = j.EmployeeId
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<Employee> GetByIdAsync(int id)
        {
            return await _dbContext.Employees
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new Employee
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Phone = e.Phone,
                    Salary = e.Salary,
                    Jobs = e.Jobs.Select(j => new Job
                    {
                        Id = j.Id,
                        Title = j.Title,
                        Description = j.Description,
                        EmployeeId = j.EmployeeId
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<Job>> GetJobsByEmployeeIdAsync(int employeeId)
        {
            return await _dbContext.Jobs
                .AsNoTracking()
                .Where(j => j.EmployeeId == employeeId)
                .Select(j => new Job
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    EmployeeId = j.EmployeeId
                })
                .ToListAsync();
        }

        public async Task<Employee> AddAsync(Employee employee)
        {
            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee> UpdateAsync(Employee employee)
        {
            var existing = await _dbContext.Employees.FindAsync(employee.Id);
            if (existing == null) return null;

            existing.Name = employee.Name;
            existing.Email = employee.Email;
            existing.Phone = employee.Phone;
            existing.Salary = employee.Salary;
            await _dbContext.SaveChangesAsync();
            return existing;
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
                .AsNoTracking()
                .Where(e => e.Salary >= minSalary)
                .Select(e => new Employee
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Phone = e.Phone,
                    Salary = e.Salary,
                    Jobs = e.Jobs.Select(j => new Job
                    {
                        Id = j.Id,
                        Title = j.Title,
                        Description = j.Description,
                        EmployeeId = j.EmployeeId
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<List<Employee>> SearchByNameAsync(string name)
        {
            return await _dbContext.Employees
                .AsNoTracking()
                .Where(e => e.Name.ToLower().Contains(name.ToLower()))
                .Select(e => new Employee
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Phone = e.Phone,
                    Salary = e.Salary,
                    Jobs = e.Jobs.Select(j => new Job
                    {
                        Id = j.Id,
                        Title = j.Title,
                        Description = j.Description,
                        EmployeeId = j.EmployeeId
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<Job> AddJobAsync(Job job)
        {
            _dbContext.Jobs.Add(job);
            await _dbContext.SaveChangesAsync();
            return job;
        }

        public async Task<Job> UpdateJobAsync(Job job)
        {
            var existing = await _dbContext.Jobs.FindAsync(job.Id);
            if (existing == null) return null;

            existing.Title = job.Title;
            existing.Description = job.Description;
            existing.EmployeeId = job.EmployeeId;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteJobAsync(int jobId)
        {
            var job = await _dbContext.Jobs.FindAsync(jobId);
            if (job == null) return false;

            _dbContext.Jobs.Remove(job);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}