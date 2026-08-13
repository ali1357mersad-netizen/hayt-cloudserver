using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// نوع عملیات موجود در صف همگام‌سازی.
    /// </summary>
    public enum CloudSyncOperationType
    {
        Create = 1,
        Update = 2,
        Delete = 3
    }

    /// <summary>
    /// وضعیت فعلی سرویس Cloud Sync.
    /// </summary>
    public enum CloudSyncState
    {
        Offline = 0,
        Ready = 1,
        Synchronizing = 2,
        PremiumRequired = 3,
        Failed = 4
    }

    /// <summary>
    /// یک عملیات مستقل از دیتابیس در صف محلی همگام‌سازی.
    /// </summary>
    public sealed class CloudSyncQueueItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string EntityType { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public CloudSyncOperationType OperationType { get; set; }

        public string PayloadJson { get; set; } = "{}";

        public DateTimeOffset CreatedAtUtc { get; set; } =
            DateTimeOffset.UtcNow;

        public int RetryCount { get; set; }

        public DateTimeOffset? LastAttemptAtUtc { get; set; }

        public string? LastError { get; set; }

        public bool IsPermanentlyFailed { get; set; }
    }

    /// <summary>
    /// نتیجه اجرای Cloud Sync.
    /// </summary>
    public sealed class CloudSyncResult
    {
        public CloudSyncResult(
            bool isSuccessful,
            CloudSyncState state,
            string message,
            int pendingItems)
        {
            IsSuccessful = isSuccessful;
            State = state;
            Message = message ?? string.Empty;
            PendingItems = pendingItems;
        }

        public bool IsSuccessful { get; }

        public CloudSyncState State { get; }

        public string Message { get; }

        public int PendingItems { get; }
    }
}
