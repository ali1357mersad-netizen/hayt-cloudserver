using System;
using System.Collections.Generic;

namespace Hayt.Shared.Cloud.Sync;

public sealed class CloudSyncItemDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CloudSyncPushRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public List<CloudSyncItemDto> Items { get; set; } = new();
}

public sealed class CloudSyncPushResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Guid> AcceptedIds { get; set; } = new();
    public List<CloudSyncRejectedItemDto> RejectedItems { get; set; } = new();
    public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CloudSyncRejectedItemDto
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class CloudSyncPullRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset? SinceUtc { get; set; }
}

public sealed class CloudSyncPullResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CloudSyncItemDto> Items { get; set; } = new();
    public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;
}
