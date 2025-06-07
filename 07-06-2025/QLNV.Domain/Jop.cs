using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Domain
{
    public class Job
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
    }
}
