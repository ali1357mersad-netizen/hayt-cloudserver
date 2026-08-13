using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// سرویس Offline-first و Premium-only.
    /// در این مرحله عمداً هیچ درخواست HTTP اجرا نمی‌شود.
    /// </summary>
    public sealed class OfflineFirstCloudSyncService :
        ICloudSyncService
    {
        private readonly EncryptedCloudSyncQueue _queue;
        private readonly Func<bool> _hasPremiumAccess;
        private readonly Func<bool> _isOnline;

        public OfflineFirstCloudSyncService(
            EncryptedCloudSyncQueue queue,
            Func<bool> hasPremiumAccess,
            Func<bool> isOnline)
        {
            _queue = queue ??
                throw new ArgumentNullException(nameof(queue));

            _hasPremiumAccess = hasPremiumAccess ??
                throw new ArgumentNullException(
                    nameof(hasPremiumAccess));

            _isOnline = isOnline ??
                throw new ArgumentNullException(nameof(isOnline));
        }

        public CloudSyncState State { get; private set; } =
            CloudSyncState.Offline;

        public async Task EnqueueAsync(
            CloudSyncQueueItem item,
            CancellationToken cancellationToken = default)
        {
            await _queue.AddAsync(
                    item,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public Task<IReadOnlyList<CloudSyncQueueItem>>
            GetPendingAsync(
                CancellationToken cancellationToken = default)
        {
            return _queue.ReadAsync(cancellationToken);
        }

        public async Task<CloudSyncResult> SynchronizeAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CloudSyncQueueItem> pending =
                await _queue.ReadAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (!_hasPremiumAccess())
            {
                State = CloudSyncState.PremiumRequired;

                return new CloudSyncResult(
                    false,
                    State,
                    "Cloud Sync فقط برای کاربران Premium فعال است.",
                    pending.Count);
            }

            if (!_isOnline())
            {
                State = CloudSyncState.Offline;

                return new CloudSyncResult(
                    false,
                    State,
                    "اینترنت در دسترس نیست؛ اطلاعات در صف امن باقی ماند.",
                    pending.Count);
            }

            /*
             * سیاست Fail-Closed:
             * تا زمان اضافه‌شدن سرور معتبر، احراز هویت،
             * TLS و مدیریت تعارض، هیچ داده‌ای ارسال نمی‌شود
             * و هیچ آیتمی از صف حذف نخواهد شد.
             */

            State = CloudSyncState.Ready;

            return new CloudSyncResult(
                true,
                State,
                "زیرساخت Cloud Sync آماده است؛ ارسال آنلاین هنوز فعال نیست.",
                pending.Count);
        }
    }
}
