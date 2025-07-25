﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Contracts.DTOs
{
    public class EmployeeDTO
    {
            public int EmployeeId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public int? DepartmentId { get; set; }
            public int? PositionId { get; set; }
            public string DepartmentName { get; set; }
            public string PositionName { get; set; }
    }
}
