using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Results
{
    public class OperationResult
    {
        public bool IsSuccess { get; protected set; }
        public string? Message { get; protected set; }

        public static OperationResult Success(string message = "")
            => new OperationResult { IsSuccess = true, Message = message };

        public static OperationResult Failure(string message)
            => new OperationResult { IsSuccess = false, Message = message };
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; private set; }

        public static OperationResult<T> Success(T data, string message = "")
            => new OperationResult<T> { IsSuccess = true, Data = data, Message = message };

        public new static OperationResult<T> Failure(string message)
            => new OperationResult<T> { IsSuccess = false, Message = message };
    }
}
