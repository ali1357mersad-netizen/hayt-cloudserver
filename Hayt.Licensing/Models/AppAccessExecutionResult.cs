using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

/// <summary>
/// نتیجه اجرای امن یک عملیات محافظت‌شده.
/// این مدل برای UI مناسب است چون به‌جای Crash، پیام قابل نمایش برمی‌گرداند.
/// </summary>
public sealed class AppAccessExecutionResult
{
    public bool Succeeded { get; init; }

    public bool AccessDenied { get; init; }

    public bool Failed { get; init; }

    public AppFeature Feature { get; init; }

    public AppAccessDecision? Decision { get; init; }

    public string Message { get; init; } = string.Empty;

    public Exception? Exception { get; init; }

    public static AppAccessExecutionResult Success(
        AppFeature feature,
        AppAccessDecision decision,
        string? message = null)
    {
        return new AppAccessExecutionResult
        {
            Succeeded = true,
            AccessDenied = false,
            Failed = false,
            Feature = feature,
            Decision = decision,
            Message = string.IsNullOrWhiteSpace(message)
                ? decision.Message
                : message,
            Exception = null
        };
    }

    public static AppAccessExecutionResult Denied(
        AppFeature feature,
        AppAccessDecision decision)
    {
        return new AppAccessExecutionResult
        {
            Succeeded = false,
            AccessDenied = true,
            Failed = false,
            Feature = feature,
            Decision = decision,
            Message = decision.Message,
            Exception = null
        };
    }

    public static AppAccessExecutionResult Error(
        AppFeature feature,
        string message,
        Exception exception)
    {
        return new AppAccessExecutionResult
        {
            Succeeded = false,
            AccessDenied = false,
            Failed = true,
            Feature = feature,
            Decision = null,
            Message = message,
            Exception = exception
        };
    }
}

public sealed class AppAccessExecutionResult<T>
{
    public bool Succeeded { get; init; }

    public bool AccessDenied { get; init; }

    public bool Failed { get; init; }

    public AppFeature Feature { get; init; }

    public T? Value { get; init; }

    public AppAccessDecision? Decision { get; init; }

    public string Message { get; init; } = string.Empty;

    public Exception? Exception { get; init; }

    public static AppAccessExecutionResult<T> Success(
        AppFeature feature,
        AppAccessDecision decision,
        T value,
        string? message = null)
    {
        return new AppAccessExecutionResult<T>
        {
            Succeeded = true,
            AccessDenied = false,
            Failed = false,
            Feature = feature,
            Value = value,
            Decision = decision,
            Message = string.IsNullOrWhiteSpace(message)
                ? decision.Message
                : message,
            Exception = null
        };
    }

    public static AppAccessExecutionResult<T> Denied(
        AppFeature feature,
        AppAccessDecision decision)
    {
        return new AppAccessExecutionResult<T>
        {
            Succeeded = false,
            AccessDenied = true,
            Failed = false,
            Feature = feature,
            Value = default,
            Decision = decision,
            Message = decision.Message,
            Exception = null
        };
    }

    public static AppAccessExecutionResult<T> Error(
        AppFeature feature,
        string message,
        Exception exception)
    {
        return new AppAccessExecutionResult<T>
        {
            Succeeded = false,
            AccessDenied = false,
            Failed = true,
            Feature = feature,
            Value = default,
            Decision = null,
            Message = message,
            Exception = exception
        };
    }
}

