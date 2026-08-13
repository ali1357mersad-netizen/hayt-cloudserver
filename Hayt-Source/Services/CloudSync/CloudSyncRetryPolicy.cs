using System;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// سیاست Retry با Backoff.
    /// </summary>
    public static class CloudSyncRetryPolicy
    {
        /// <summary>
        /// حداکثر تعداد تلاش‌ها.
        /// </summary>
        public const int MaxRetryCount = 5;

        /// <summary>
        /// محاسبه تأخیر Backoff بر اساس تعداد تلاش.
        /// </summary>
        public static TimeSpan GetBackoffDelay(int retryCount)
        {
            return retryCount switch
            {
                0 => TimeSpan.FromSeconds(5),
                1 => TimeSpan.FromSeconds(15),
                2 => TimeSpan.FromSeconds(30),
                3 => TimeSpan.FromSeconds(60),
                _ => TimeSpan.FromMinutes(2)
            };
        }

        /// <summary>
        /// آیا آیتم هنوز می‌تواند Retry شود؟
        /// </summary>
        public static bool CanRetry(CloudSyncQueueItem item)
        {
            if (item is null)
            {
                return false;
            }

            if (item.IsPermanentlyFailed)
            {
                return false;
            }

            return item.RetryCount < MaxRetryCount;
        }

        /// <summary>
        /// آیا زمان Retry فرا رسیده است؟
        /// </summary>
        public static bool IsRetryDue(
            CloudSyncQueueItem item,
            DateTimeOffset now)
        {
            if (item is null)
            {
                return false;
            }

            if (item.LastAttemptAtUtc is null)
            {
                return true;
            }

            TimeSpan delay = GetBackoffDelay(item.RetryCount);

            return now >= item.LastAttemptAtUtc.Value.Add(delay);
        }
    }
}
