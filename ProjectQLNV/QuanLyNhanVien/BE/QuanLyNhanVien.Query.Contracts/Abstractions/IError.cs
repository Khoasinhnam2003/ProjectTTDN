using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Contracts.Abstractions
{
    public interface IError
    {
        string Message { get; }
    }
}
