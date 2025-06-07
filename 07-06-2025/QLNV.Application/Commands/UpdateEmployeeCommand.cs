using MediatR;
using QLNV.Application.DTOs;
using QLNV.Domain;
using QLNV.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Application.Commands
{
    public class UpdateEmployeeCommand : IRequest
    {
        public int Id { get; set; }
        public UpdateEmployeeDto EmployeeDto { get; set; }
    }

    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public UpdateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.GetByIdAsync(request.Id);
            if (employee == null)
                throw new ArgumentException($"Nhân viên với Id {request.Id} không tồn tại");

            employee.Name = request.EmployeeDto.Name;
            employee.Email = request.EmployeeDto.Email;
            employee.Phone = request.EmployeeDto.Phone;
            employee.Salary = request.EmployeeDto.Salary;

            // Xóa các công việc cũ
            employee.Jobs.Clear();
            // Thêm các công việc mới từ DTO
            foreach (var jobDto in request.EmployeeDto.Jobs)
            {
                employee.Jobs.Add(new Job
                {
                    Id = jobDto.Id,
                    Title = jobDto.Title,
                    Description = jobDto.Description
                });
            }

            await _employeeRepository.UpdateAsync(employee);
        }
    }
}
