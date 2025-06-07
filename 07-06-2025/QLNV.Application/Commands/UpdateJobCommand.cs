using MediatR;
using QLNV.Application.DTOs;
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
}
