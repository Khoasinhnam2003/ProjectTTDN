using System;
using System.Collections.Generic;

namespace QuanLyNhanVien.Domain.Entities;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateOnly HireDate { get; set; }

    public int? DepartmentId { get; set; }

    public int? PositionId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual Position? Position { get; set; }

    public virtual ICollection<SalaryHistory> SalaryHistories { get; set; } = new List<SalaryHistory>();

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
