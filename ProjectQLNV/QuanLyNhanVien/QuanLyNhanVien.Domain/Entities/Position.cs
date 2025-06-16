using System;
using System.Collections.Generic;

namespace QuanLyNhanVien.Domain.Entities;

public partial class Position
{
    public int PositionId { get; set; }

    public string PositionName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? BaseSalary { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
