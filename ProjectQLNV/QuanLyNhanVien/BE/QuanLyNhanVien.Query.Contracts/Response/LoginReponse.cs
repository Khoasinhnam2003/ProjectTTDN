using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Domain.Response
{
    public record LoginResponse
    {
            public int UserId { get; set; }
            public int EmployeeId { get; set; }
            public string Username { get; set; }
            public string Token { get; set; }
            public List<string> Roles { get; set; } = new List<string>();
    }
}
