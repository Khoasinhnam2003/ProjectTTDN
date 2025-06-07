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

            foreach (var jobDto in addEmployeeDto.Jobs)
            {
                employee.Jobs.Add(new Job
                {
                    Title = jobDto.Title,
                    Description = jobDto.Description
                });
            }

            return await _employeeRepository.AddAsync(employee);
        }

        public async Task<Employee> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto)
        {
            var existingEmployee = await _employeeRepository.GetByIdAsync(id);
            if (existingEmployee == null)
                throw new ArgumentException($"Nhân viên với Id {id} không tồn tại");

            existingEmployee.Name = updateEmployeeDto.Name;
            existingEmployee.Email = updateEmployeeDto.Email;
            existingEmployee.Phone = updateEmployeeDto.Phone;
            existingEmployee.Salary = updateEmployeeDto.Salary;

            // Xóa các công việc cũ
            existingEmployee.Jobs.Clear();
            // Thêm các công việc mới từ DTO
            foreach (var jobDto in updateEmployeeDto.Jobs)
            {
                existingEmployee.Jobs.Add(new Job
                {
                    Id = jobDto.Id,
                    Title = jobDto.Title,
                    Description = jobDto.Description
                });
            }

            return await _employeeRepository.UpdateAsync(existingEmployee);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var success = await _employeeRepository.DeleteAsync(id);
            if (!success)
                throw new ArgumentException($"Nhân viên với Id {id} không tồn tại");

            return true;
        }

        public async Task<List<Employee>> GetEmployeesBySalaryAsync(decimal minSalary)
        {
            if (minSalary < 0)
                throw new ArgumentException("Lương tối thiểu không thể âm");

            return await _employeeRepository.GetBySalaryAsync(minSalary);
        }

        public async Task<List<Employee>> SearchEmployeesByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Tên không được để trống");

            return await _employeeRepository.SearchByNameAsync(name);
        }

        public async Task<Job> AddJobAsync(int employeeId, AddJobDto addJobDto)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
                throw new ArgumentException($"Nhân viên với Id {employeeId} không tồn tại");

            var job = new Job
            {
                Title = addJobDto.Title,
                Description = addJobDto.Description,
                EmployeeId = employeeId
            };

            return await _employeeRepository.AddJobAsync(job);
        }

        public async Task<List<Job>> GetJobsByEmployeeIdAsync(int employeeId)
        {
            var jobs = await _employeeRepository.GetJobsByEmployeeIdAsync(employeeId);
            if (jobs == null || jobs.Count == 0)
                throw new ArgumentException($"Không tìm thấy công việc cho nhân viên với Id {employeeId}");

            return jobs;
        }

        public async Task<Job> UpdateJobAsync(int jobId, UpdateJobDto updateJobDto)
        {
            var job = new Job
            {
                Id = jobId,
                Title = updateJobDto.Title,
                Description = updateJobDto.Description,
                EmployeeId = updateJobDto.Id // Giả định UpdateJobDto có Id của Employee
            };

            var updatedJob = await _employeeRepository.UpdateJobAsync(job);
            if (updatedJob == null)
                throw new ArgumentException($"Công việc với Id {jobId} không tồn tại");

            return updatedJob;
        }

        public async Task<bool> DeleteJobAsync(int jobId)
        {
            var success = await _employeeRepository.DeleteJobAsync(jobId);
            if (!success)
                throw new ArgumentException($"Công việc với Id {jobId} không tồn tại");

            return true;
        }
    }
}