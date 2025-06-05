using MediatR;
using QLNV.Domain;
using QLNV.Infrastructure.Repositories;


namespace QLNV.Application.Commands
{
        public class CreateEmployeeCommand : IRequest<Employee>
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public decimal Salary { get; set; }
        }

        public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Employee>
        {
            private readonly IEmployeeRepository _employeeRepository;

            public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
            {
                _employeeRepository = employeeRepository;
            }

            public async Task<Employee> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
            {
                var employee = new Employee
                {
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.Phone,
                    Salary = request.Salary
                };
                await _employeeRepository.AddAsync(employee);

                return employee;
            }
        }
}
