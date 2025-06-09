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
    public class UpdateJobCommand : IRequest
    {
        public int JobId { get; set; }
        public int EmployeeId { get; set; }
        public UpdateJobDto JobDto { get; set; }
    }

    public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public UpdateJobCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
        {
            // Tìm công việc theo JobId
            var job = new Job
            {
                Id = request.JobId,
                Title = request.JobDto.Title,
                Description = request.JobDto.Description,
                EmployeeId = request.EmployeeId
            };

            var updatedJob = await _employeeRepository.UpdateJobAsync(job);
            if (updatedJob == null)
                throw new ArgumentException($"Công việc với Id {request.JobId} không tồn tại");
        }
    }
}
