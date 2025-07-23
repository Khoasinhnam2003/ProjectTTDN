using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Contracts.DTOs
{
    public class SalaryHistoryDTO
    {
        public int SalaryHistoryId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal Salary { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
