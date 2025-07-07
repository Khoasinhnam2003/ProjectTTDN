using QuanLyNhanVien.Query.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Contracts.Errors
{
    public class Error : IError
    {
        public string Message { get; }

        public Error(string message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
