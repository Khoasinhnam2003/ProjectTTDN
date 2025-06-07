using MediatR;
using QLNV.Application.DTOs;
using QLNV.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Application.Queries
{
    public class GetJobsByEmployeeIdQuery : IRequest<List<JobDTO>>
    {
        public int EmployeeId { get; set; }
    }

    public class GetJobsByEmployeeIdQueryHandler : IRequestHandler<GetJobsByEmployeeIdQuery, List<JobDTO>>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public GetJobsByEmployeeIdQueryHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<List<JobDTO>> Handle(GetJobsByEmployeeIdQuery request, CancellationToken cancellationToken)
        {
            var jobs = await _employeeRepository.GetJobsByEmployeeIdAsync(request.EmployeeId);
            return jobs.Select(j => new JobDTO
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                EmployeeId = j.EmployeeId
            }).ToList();
        }
    }
}
