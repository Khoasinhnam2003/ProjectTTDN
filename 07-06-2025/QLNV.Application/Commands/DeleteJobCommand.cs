using MediatR;
using QLNV.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Application.Commands
{
    public class DeleteJobCommand : IRequest
    {
        public int JobId { get; set; }
        public int EmployeeId { get; set; }
    }

    public class DeleteJobCommandHandler : IRequestHandler<DeleteJobCommand>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public DeleteJobCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task Handle(DeleteJobCommand request, CancellationToken cancellationToken)
        {
            var success = await _employeeRepository.DeleteJobAsync(request.JobId);
            if (!success)
                throw new ArgumentException($"Công việc với Id {request.JobId} không tồn tại hoặc không thuộc nhân viên với Id {request.EmployeeId}");
        }
    }
}
