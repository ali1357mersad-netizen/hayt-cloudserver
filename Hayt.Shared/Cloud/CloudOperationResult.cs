using System;

namespace Hayt.Shared.Cloud;

public sealed class CloudOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }

    public static CloudOperationResult Ok(string message = "")
    {
        return new CloudOperationResult
        {
            Success = true,
            Message = message
        };
    }

    public static CloudOperationResult Fail(string message, Exception? exception = null)
    {
        return new CloudOperationResult
        {
            Success = false,
            Message = message,
            Exception = exception
        };
    }
}

public sealed class CloudOperationResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Exception? Exception { get; set; }

    public static CloudOperationResult<T> Ok(T data, string message = "")
    {
        return new CloudOperationResult<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static CloudOperationResult<T> Fail(string message, Exception? exception = null)
    {
        return new CloudOperationResult<T>
        {
            Success = false,
            Message = message,
            Exception = exception
        };
    }
}
