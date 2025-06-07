using MediatR;
using QLNV.Application.DTOs;
using QLNV.Domain;
using QLNV.Infrastructure.Repositories;


namespace QLNV.Application.Commands
{
    public class CreateEmployeeCommand : IRequest<EmployeeDTO>
    {
        public AddEmployeeDto EmployeeDto { get; set; }
    }

    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDTO>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<EmployeeDTO> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = new QLNV.Domain.Employee
            {
                Name = request.EmployeeDto.Name,
                Email = request.EmployeeDto.Email,
                Phone = request.EmployeeDto.Phone,
                Salary = request.EmployeeDto.Salary
            };

            var addedEmployee = await _employeeRepository.AddAsync(employee);

            var jobs = request.EmployeeDto.Jobs.Select(j => new QLNV.Domain.Job
            {
                Title = j.Title,
                Description = j.Description,
                EmployeeId = addedEmployee.Id
            }).ToList();

            foreach (var job in jobs)
            {
                await _employeeRepository.AddJobAsync(job);
            }

            return new EmployeeDTO
            {
                Id = addedEmployee.Id,
                Name = addedEmployee.Name,
                Email = addedEmployee.Email,
                Phone = addedEmployee.Phone,
                Salary = addedEmployee.Salary,
                Jobs = jobs.Select(j => new JobDTO
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    EmployeeId = j.EmployeeId
                }).ToList()
            };
        }
    }
}
