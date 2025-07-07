using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Contracts.Abstractions
{
    public interface IResult
    {
        bool IsSuccess { get; }
        string Error { get; }
    }

    public interface IResult<T> : IResult
    {
        T Data { get; }
    }
}
