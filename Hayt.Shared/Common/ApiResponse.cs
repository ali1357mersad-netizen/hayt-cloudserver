using System;

namespace Hayt.Shared.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Status { get; set; } = "success";
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "OK")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Status = "success",
            Message = message,
            Data = data,
            ServerTimeUtc = DateTimeOffset.UtcNow
        };
    }

    public static ApiResponse<T> Fail(string message, string status = "error")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Status = status,
            Message = message,
            Data = default,
            ServerTimeUtc = DateTimeOffset.UtcNow
        };
    }
}
