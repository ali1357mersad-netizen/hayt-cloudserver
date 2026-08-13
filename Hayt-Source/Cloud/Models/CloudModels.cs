using System;
using System.Collections.Generic;

namespace Hayt.Cloud.Models
{
    public sealed class CloudSyncItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = "{}";
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class CloudSyncPushRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public List<CloudSyncItem> Items { get; set; } = new();
    }

    public sealed class CloudSyncPushResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<Guid> AcceptedIds { get; set; } = new();
        public List<CloudSyncRejectedItem> RejectedItems { get; set; } = new();
        public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class CloudSyncRejectedItem
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class CloudSyncPullRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public DateTimeOffset? SinceUtc { get; set; }
    }

    public sealed class CloudSyncPullResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CloudSyncItem> Items { get; set; } = new();
        public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class ServerHealthResponse
    {
        public bool Ok { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class OnlineUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTimeOffset ConnectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class OnlineMessageDto
    {
        public string FromUserId { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class CloudOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }

        public static CloudOperationResult Ok(string message = "")
            => new() { Success = true, Message = message };

        public static CloudOperationResult Fail(string message, Exception? exception = null)
            => new() { Success = false, Message = message, Exception = exception };
    }

    public sealed class CloudOperationResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public Exception? Exception { get; set; }

        public static CloudOperationResult<T> Ok(T data, string message = "")
            => new() { Success = true, Message = message, Data = data };

        public static CloudOperationResult<T> Fail(string message, Exception? exception = null)
            => new() { Success = false, Message = message, Exception = exception };
    }

    public sealed class CloudClientOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:5088";
        public string ApiKey { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DeviceId { get; set; } = Environment.MachineName;
        public int TimeoutSeconds { get; set; } = 20;
        public int MaxRetryCount { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 700;
    }

    public enum CloudConnectionState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Synchronizing = 3,
        Failed = 4
    }
}