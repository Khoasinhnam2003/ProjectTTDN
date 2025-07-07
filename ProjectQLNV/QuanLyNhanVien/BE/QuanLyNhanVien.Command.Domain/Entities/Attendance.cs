using System;
using System.Collections.Generic;

namespace QuanLyNhanVien.Command.Domain.Entities;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
