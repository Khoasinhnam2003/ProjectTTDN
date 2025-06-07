using Microsoft.EntityFrameworkCore;
using QLNV.Domain;
using QLNV.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Infrastructure.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public JobRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Job>> GetJobsByEmployeeIdAsync(int employeeId)
        {
            return await _dbContext.Jobs
                .Where(j => j.EmployeeId == employeeId)
                .Select(j => new Job
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    EmployeeId = j.EmployeeId
                })
                .ToListAsync();
        }
    }
}
