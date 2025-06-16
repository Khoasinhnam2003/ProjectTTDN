using System;
using System.Collections.Generic;

namespace QuanLyNhanVien.Domain.Entities;

public partial class Skill
{
    public int SkillId { get; set; }

    public int EmployeeId { get; set; }

    public string SkillName { get; set; } = null!;

    public string? ProficiencyLevel { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
