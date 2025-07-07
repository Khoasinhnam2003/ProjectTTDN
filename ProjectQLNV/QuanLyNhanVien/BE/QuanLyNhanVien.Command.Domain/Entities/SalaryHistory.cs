using System;
using System.Collections.Generic;

namespace QuanLyNhanVien.Command.Domain.Entities;

public partial class SalaryHistory
{
    public int SalaryHistoryId { get; set; }

    public int EmployeeId { get; set; }

    public decimal Salary { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
