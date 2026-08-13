namespace Hayt.CloudServer.Models;

public sealed class HealthResponse
{
    public string Status { get; init; } = "Healthy";
    public string Service { get; init; } = "Hayt.CloudServer";
    public string Version { get; init; } = "1.1.0";
    public DateTimeOffset ServerTimeUtc { get; init; } = DateTimeOffset.UtcNow;
    public long UptimeSeconds { get; init; }
    public int OnlineUsers { get; init; }
    public string Environment { get; init; } = string.Empty;
}

public sealed class OnlineUser
{
    public string ConnectionId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTimeOffset ConnectedAtUtc { get; init; }
    public DateTimeOffset LastSeenUtc { get; set; }
    public string Platform { get; init; } = "Hayt-WPF";
}

public sealed class LoginRequest
{
    public string UserId { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public string LoginKey { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class LoginResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public UserInfo? User { get; init; }
}

public sealed class UserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class CloudMessageRequest
{
    public string? UserId { get; init; }
    public string? Message { get; init; }
    public string? Type { get; init; }
    public object? Payload { get; init; }
}

public sealed class SyncPushRequest
{
    public string? UserId { get; init; }
    public string? DeviceId { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public DateTimeOffset? ClientTimeUtc { get; init; }
    public object? Data { get; init; }
}

public sealed class StoredSyncItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string UserId { get; init; } = "local-user";
    public string DeviceId { get; init; } = "local-device";
    public string EntityType { get; init; } = "unknown";
    public string EntityId { get; init; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public object? Data { get; init; }
}

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public DateTimeOffset ServerTimeUtc { get; init; } = DateTimeOffset.UtcNow;
}
