using QuanLyNhanVien.Query.Contracts.Abstractions;
using QuanLyNhanVien.Query.Contracts.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Contracts.Shared
{
    public class Result<T> : IResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public Error Error { get; }

        private Result(bool isSuccess, T data, Error error)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
        }

        public static Result<T> Success(T data)
        {
            return new Result<T>(true, data, null);
        }

        public static Result<T> Failure(Error error)
        {
            return new Result<T>(false, default, error);
        }

        string IResult.Error => Error?.Message ?? string.Empty;
    }
}
