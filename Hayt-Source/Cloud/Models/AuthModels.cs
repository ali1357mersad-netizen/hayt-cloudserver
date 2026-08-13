using System.Text.Json.Serialization;

namespace Hayt.Cloud.Models;

/// <summary>
/// درخواست ورود به سرور Cloud.
/// </summary>
public sealed class LoginRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; init; } = string.Empty;

    [JsonPropertyName("loginKey")]
    public string LoginKey { get; init; } = string.Empty;
}

/// <summary>
/// پاسخ ورود و اطلاعات توکن صادرشده توسط سرور.
/// </summary>
public sealed class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; init; } = string.Empty;
}