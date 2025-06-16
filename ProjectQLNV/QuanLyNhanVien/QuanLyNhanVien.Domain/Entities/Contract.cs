using System;
using System.Collections.Generic;

namespace QuanLyNhanVien.Domain.Entities;

public partial class Contract
{
    public int ContractId { get; set; }

    public int EmployeeId { get; set; }

    public string ContractType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal Salary { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
