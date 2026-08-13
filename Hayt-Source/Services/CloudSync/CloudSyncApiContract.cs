using System;
using System.Collections.Generic;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// نسخه فعلی قرارداد API.
    /// </summary>
    public static class CloudSyncApiVersions
    {
        public const string V1 = "v1";

        public const string Current = V1;
    }

    /// <summary>
    /// مسیرهای قرارداد API همگام‌سازی.
    /// </summary>
    public static class CloudSyncApiRoutes
    {
        public const string BasePath = "/api/" + CloudSyncApiVersions.Current + "/sync";

        /// <summary>
        /// ارسال بسته داده به سرور.
        /// </summary>
        public const string Batch = BasePath + "/batch";

        /// <summary>
        /// دریافت تغییرات سرور.
        /// </summary>
        public const string Changes = BasePath + "/changes";

        /// <summary>
        /// تأیید دریافت تغییرات.
        /// </summary>
        public const string Ack = BasePath + "/ack";

        /// <summary>
        /// دریافت وضعیت همگام‌سازی.
        /// </summary>
        public const string Status = BasePath + "/status";
    }

    /// <summary>
    /// نوع عملیات در قرارداد API.
    /// </summary>
    public enum CloudSyncApiOperationType
    {
        Create = 1,
        Update = 2,
        Delete = 3
    }

    /// <summary>
    /// وضعیت پاسخ سرور.
    /// </summary>
    public enum CloudSyncApiStatus
    {
        Success = 0,
        PartialSuccess = 1,
        Failed = 2,
        Unauthorized = 3,
        Conflict = 4,
        RateLimited = 5,
        ServerError = 6
    }

    /// <summary>
    /// یک آیتم همگام‌سازی در قرارداد API.
    /// </summary>
    public sealed class CloudSyncItemDto
    {
        /// <summary>
        /// شناسه یکتای عملیات (در کلاینت).
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// نوع موجودیت (مثلاً Note، LessonProgress، Certificate).
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// شناسه موجودیت در کلاینت.
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// نوع عملیات.
        /// </summary>
        public CloudSyncApiOperationType OperationType { get; set; }

        /// <summary>
        /// داده‌های عملیات به‌صورت JSON.
        /// </summary>
        public string PayloadJson { get; set; } = "{}";

        /// <summary>
        /// زمان ایجاد عملیات در کلاینت (UTC).
        /// </summary>
        public DateTimeOffset CreatedAtUtc { get; set; }

        /// <summary>
        /// نسخه آخرین تغییر موجودیت (برای مدیریت تعارض).
        /// </summary>
        public long Version { get; set; }
    }

    /// <summary>
    /// بسته داده ارسالی به سرور.
    /// </summary>
    public sealed class CloudSyncBatchDto
    {
        /// <summary>
        /// شناسه یکتای بسته.
        /// </summary>
        public Guid BatchId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// شناسه دستگاه کلاینت.
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// شناسه کاربر (در صورت احراز هویت).
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// زمان ارسال بسته (UTC).
        /// </summary>
        public DateTimeOffset SentAtUtc { get; set; } =
            DateTimeOffset.UtcNow;

        /// <summary>
        /// آیتم‌های موجود در بسته.
        /// </summary>
        public List<CloudSyncItemDto> Items { get; set; } =
            new List<CloudSyncItemDto>();
    }

    /// <summary>
    /// نتیجه پردازش یک آیتم در سرور.
    /// </summary>
    public sealed class CloudSyncItemResultDto
    {
        /// <summary>
        /// شناسه عملیات در کلاینت.
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// آیا پردازش موفق بود؟
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// کد خطا (در صورت ناموفق بودن).
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// پیام خطا (در صورت ناموفق بودن).
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// نسخه جدید موجودیت در سرور.
        /// </summary>
        public long NewVersion { get; set; }
    }

    /// <summary>
    /// پاسخ سرور به بسته داده.
    /// </summary>
    public sealed class CloudSyncBatchResponseDto
    {
        /// <summary>
        /// شناسه بسته ارسالی.
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// وضعیت کلی پاسخ.
        /// </summary>
        public CloudSyncApiStatus Status { get; set; }

        /// <summary>
        /// نتیجه پردازش هر آیتم.
        /// </summary>
        public List<CloudSyncItemResultDto> ItemResults { get; set; } =
            new List<CloudSyncItemResultDto>();

        /// <summary>
        /// پیام کلی (در صورت نیاز).
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// یک تغییر از سمت سرور.
    /// </summary>
    public sealed class CloudSyncServerChangeDto
    {
        /// <summary>
        /// شناسه یکتای تغییر در سرور.
        /// </summary>
        public Guid ChangeId { get; set; }

        /// <summary>
        /// نوع موجودیت.
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// شناسه موجودیت در سرور.
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// نوع عملیات.
        /// </summary>
        public CloudSyncApiOperationType OperationType { get; set; }

        /// <summary>
        /// داده‌های تغییر به‌صورت JSON.
        /// </summary>
        public string PayloadJson { get; set; } = "{}";

        /// <summary>
        /// زمان ایجاد تغییر در سرور (UTC).
        /// </summary>
        public DateTimeOffset ChangedAtUtc { get; set; }

        /// <summary>
        /// نسخه تغییر در سرور.
        /// </summary>
        public long Version { get; set; }
    }

    /// <summary>
    /// پاسخ دریافت تغییرات سرور.
    /// </summary>
    public sealed class CloudSyncChangesResponseDto
    {
        /// <summary>
        /// وضعیت پاسخ.
        /// </summary>
        public CloudSyncApiStatus Status { get; set; }

        /// <summary>
        /// تغییرات سرور.
        /// </summary>
        public List<CloudSyncServerChangeDto> Changes { get; set; } =
            new List<CloudSyncServerChangeDto>();

        /// <summary>
        /// نشانگر ادامه برای دریافت تغییرات بعدی.
        /// </summary>
        public string? NextCursor { get; set; }

        /// <summary>
        /// آیا تغییرات بیشتری وجود دارد؟
        /// </summary>
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// درخواست تأیید دریافت تغییرات.
    /// </summary>
    public sealed class CloudSyncAckRequestDto
    {
        /// <summary>
        /// شناسه تغییرات تأییدشده.
        /// </summary>
        public List<Guid> ChangeIds { get; set; } =
            new List<Guid>();

        /// <summary>
        /// شناسه دستگاه کلاینت.
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;
    }

    /// <summary>
    /// پاسخ تأیید دریافت.
    /// </summary>
    public sealed class CloudSyncAckResponseDto
    {
        /// <summary>
        /// وضعیت پاسخ.
        /// </summary>
        public CloudSyncApiStatus Status { get; set; }

        /// <summary>
        /// پیام (در صورت نیاز).
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// وضعیت همگام‌سازی از سمت سرور.
    /// </summary>
    public sealed class CloudSyncStatusDto
    {
        /// <summary>
        /// وضعیت پاسخ.
        /// </summary>
        public CloudSyncApiStatus Status { get; set; }

        /// <summary>
        /// آخرین نشانگر همگام‌سازی کلاینت.
        /// </summary>
        public string? LastCursor { get; set; }

        /// <summary>
        /// تعداد تغییرات در انتظار.
        /// </summary>
        public int PendingChangeCount { get; set; }

        /// <summary>
        /// زمان سرور (UTC).
        /// </summary>
        public DateTimeOffset ServerTimeUtc { get; set; } =
            DateTimeOffset.UtcNow;
    }
}