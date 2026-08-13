using System;

namespace Hayt.Models;

public sealed class AITutorBridgeResponse
{
    public bool IsSuccess { get; init; }

    public AITutorRequestKind Kind { get; init; } = AITutorRequestKind.Unknown;

    public string Content { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public bool UsedRealAI { get; init; }

    public bool UsedFallback { get; init; }

    public string SourceText { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public static AITutorBridgeResponse FromAIResult(
        AITutorRequestKind kind,
        AIRequestResult result)
    {
        return new AITutorBridgeResponse
        {
            IsSuccess = result.IsSuccess || !string.IsNullOrWhiteSpace(result.Content),
            Kind = kind,
            Content = result.Content ?? string.Empty,
            ErrorMessage = result.ErrorMessage ?? string.Empty,
            UsedRealAI = result.UsedRealAI,
            UsedFallback = result.UsedFallback,
            SourceText = result.SourceText,
            CreatedAt = DateTime.Now
        };
    }

    public static AITutorBridgeResponse Failure(
        AITutorRequestKind kind,
        string errorMessage)
    {
        return new AITutorBridgeResponse
        {
            IsSuccess = false,
            Kind = kind,
            Content = string.Empty,
            ErrorMessage = errorMessage ?? string.Empty,
            UsedRealAI = false,
            UsedFallback = false,
            SourceText = "خطا",
            CreatedAt = DateTime.Now
        };
    }
}