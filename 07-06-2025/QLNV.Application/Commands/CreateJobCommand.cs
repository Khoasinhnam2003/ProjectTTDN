using MediatR;
using QLNV.Application.DTOs;
using QLNV.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Application.Commands
{
    public class CreateJobCommand : IRequest<JobDTO>
    {
        public int EmployeeId { get; set; }
        public AddJobDto JobDto { get; set; }
    }

    public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDTO>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public CreateJobCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<JobDTO> Handle(CreateJobCommand request, CancellationToken cancellationToken)
        {
            var job = new QLNV.Domain.Job
            {
                Title = request.JobDto.Title,
                Description = request.JobDto.Description,
                EmployeeId = request.EmployeeId
            };

            var addedJob = await _employeeRepository.AddJobAsync(job);

            return new JobDTO
            {
                Id = addedJob.Id,
                Title = addedJob.Title,
                Description = addedJob.Description,
                EmployeeId = addedJob.Id
            };
        }
    }
}
