using System;

namespace Hayt.Models;

public sealed class AIRequestResult
{
    public bool IsSuccess { get; init; }

    public bool UsedRealAI { get; init; }

    public bool UsedFallback { get; init; }

    public string Content { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public int? StatusCode { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public string SourceText
    {
        get
        {
            if (UsedRealAI)
            {
                return "مدل واقعی";
            }

            if (UsedFallback)
            {
                return "پاسخ محلی / پشتیبان";
            }

            return "نامشخص";
        }
    }

    public static AIRequestResult Success(string content, bool usedRealAI)
    {
        return new AIRequestResult
        {
            IsSuccess = true,
            UsedRealAI = usedRealAI,
            UsedFallback = !usedRealAI,
            Content = content ?? string.Empty,
            ErrorMessage = string.Empty,
            StatusCode = null,
            CreatedAt = DateTime.Now
        };
    }

    public static AIRequestResult Failure(
        string errorMessage,
        string fallbackContent = "",
        int? statusCode = null)
    {
        return new AIRequestResult
        {
            IsSuccess = false,
            UsedRealAI = false,
            UsedFallback = !string.IsNullOrWhiteSpace(fallbackContent),
            Content = fallbackContent ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty,
            StatusCode = statusCode,
            CreatedAt = DateTime.Now
        };
    }
}