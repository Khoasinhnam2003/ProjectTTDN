using QLNV.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Infrastructure.Repositories
{
    public interface IJobRepository
    {
        Task<List<Job>> GetJobsByEmployeeIdAsync(int employeeId);
    }
}
