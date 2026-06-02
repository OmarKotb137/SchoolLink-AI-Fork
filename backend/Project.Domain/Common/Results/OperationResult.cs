namespace Common.Results
{
    public class OperationResult
    {
        public bool IsSuccess { get; protected set; }
        public string? Message { get; protected set; }
        public int StatusCode { get; protected set; }

        public static OperationResult Success(string message = "")
            => new OperationResult { IsSuccess = true, Message = message, StatusCode = 200 };

        public static OperationResult Failure(string message, int statusCode = 400)
            => new OperationResult { IsSuccess = false, Message = message, StatusCode = statusCode };
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; private set; }

        public static OperationResult<T> Success(T data, string message = "")
            => new OperationResult<T> { IsSuccess = true, Data = data, Message = message, StatusCode = 200 };

        public new static OperationResult<T> Failure(string message, int statusCode = 400)
            => new OperationResult<T> { IsSuccess = false, Message = message, StatusCode = statusCode };
    }
}