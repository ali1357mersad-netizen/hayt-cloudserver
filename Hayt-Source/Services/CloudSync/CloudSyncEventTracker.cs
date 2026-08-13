using Hayt.Licensing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// نوع موجودیت قابل رصد در Cloud Sync.
    /// </summary>
    public enum CloudSyncEntityType
    {
        StudyNote = 1,
        LessonProgress = 2,
        Certificate = 3,
        UserProfile = 4
    }

    /// <summary>
    /// قرارداد رصد رویدادهای داده برای Cloud Sync.
    /// </summary>
    public interface ICloudSyncEventTracker
    {
        /// <summary>
        /// ثبت عملیات Create برای یک موجودیت.
        /// </summary>
        Task TrackCreateAsync(
            CloudSyncEntityType entityType,
            string entityId,
            object payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// ثبت عملیات Update برای یک موجودیت.
        /// </summary>
        Task TrackUpdateAsync(
            CloudSyncEntityType entityType,
            string entityId,
            object payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// ثبت عملیات Delete برای یک موجودیت.
        /// </summary>
        Task TrackDeleteAsync(
            CloudSyncEntityType entityType,
            string entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// تعداد آیتم‌های در انتظار همگام‌سازی.
        /// </summary>
        Task<int> GetPendingCountAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// اجرای همگام‌سازی (در این مرحله فقط وضعیت را بررسی می‌کند).
        /// </summary>
        Task<CloudSyncResult> SynchronizeAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// رصدکننده رویدادهای داده.
    /// این کلاس عملیات واقعی دیتابیس را تغییر نمی‌دهد؛
    /// فقط پس از موفقیت عملیات، رویداد را در صف امن ثبت می‌کند.
    /// </summary>
    public sealed class CloudSyncEventTracker : ICloudSyncEventTracker
    {
        private readonly ICloudSyncService _cloudSyncService;
        private readonly IPremiumAccessService _premiumAccessService;
        private readonly Func<bool> _isOnline;

        public CloudSyncEventTracker(
            ICloudSyncService cloudSyncService,
            IPremiumAccessService premiumAccessService,
            Func<bool>? isOnline = null)
        {
            _cloudSyncService = cloudSyncService ??
                throw new ArgumentNullException(nameof(cloudSyncService));

            _premiumAccessService = premiumAccessService ??
                throw new ArgumentNullException(nameof(premiumAccessService));

            _isOnline = isOnline ?? (() => false);
        }

        public async Task TrackCreateAsync(
            CloudSyncEntityType entityType,
            string entityId,
            object payload,
            CancellationToken cancellationToken = default)
        {
            await TrackAsync(
                entityType,
                entityId,
                payload,
                CloudSyncOperationType.Create,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task TrackUpdateAsync(
            CloudSyncEntityType entityType,
            string entityId,
            object payload,
            CancellationToken cancellationToken = default)
        {
            await TrackAsync(
                entityType,
                entityId,
                payload,
                CloudSyncOperationType.Update,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task TrackDeleteAsync(
            CloudSyncEntityType entityType,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            await TrackAsync(
                entityType,
                entityId,
                null,
                CloudSyncOperationType.Delete,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> GetPendingCountAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CloudSyncQueueItem> pending =
                await _cloudSyncService
                    .GetPendingAsync(cancellationToken)
                    .ConfigureAwait(false);

            return pending.Count;
        }

        public async Task<CloudSyncResult> SynchronizeAsync(
            CancellationToken cancellationToken = default)
        {
            return await _cloudSyncService
                .SynchronizeAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task TrackAsync(
            CloudSyncEntityType entityType,
            string entityId,
            object? payload,
            CloudSyncOperationType operationType,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                throw new ArgumentException(
                    "شناسه موجودیت نمی‌تواند خالی باشد.",
                    nameof(entityId));
            }

            // سیاست Fail-Closed:
            // فقط کاربران Premium می‌توانند رویدادها را در صف ثبت کنند.
            // اگر دسترسی Premium نباشد، رویداد ثبت نمی‌شود.
            bool hasPremium = _premiumAccessService.CanAccess(
                PremiumFeature.CloudSync,
                forceRefresh: false);

            if (!hasPremium)
            {
                return;
            }

            string payloadJson = "{}";

            if (payload is not null)
            {
                payloadJson = JsonSerializer.Serialize(payload);
            }

            var item = new CloudSyncQueueItem
            {
                EntityType = entityType.ToString(),
                EntityId = entityId,
                OperationType = operationType,
                PayloadJson = payloadJson,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            await _cloudSyncService
                .EnqueueAsync(item, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// آداپتور اتصال Cloud Sync به رویدادهای یادداشت.
    /// این کلاس به StudyNotesService متصل می‌شود.
    /// </summary>
    public sealed class CloudSyncNotesAdapter
    {
        private readonly ICloudSyncEventTracker _tracker;

        public CloudSyncNotesAdapter(
            ICloudSyncEventTracker tracker)
        {
            _tracker = tracker ??
                throw new ArgumentNullException(nameof(tracker));
        }

        /// <summary>
        /// پس از ایجاد یادداشت جدید فراخوانی می‌شود.
        /// </summary>
        public async Task OnNoteCreatedAsync(
            StudyNote note,
            CancellationToken cancellationToken = default)
        {
            if (note is null)
            {
                return;
            }

            await _tracker.TrackCreateAsync(
                CloudSyncEntityType.StudyNote,
                note.Id,
                CreateNotePayload(note),
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// پس از ویرایش یادداشت فراخوانی می‌شود.
        /// </summary>
        public async Task OnNoteUpdatedAsync(
            StudyNote note,
            CancellationToken cancellationToken = default)
        {
            if (note is null)
            {
                return;
            }

            await _tracker.TrackUpdateAsync(
                CloudSyncEntityType.StudyNote,
                note.Id,
                CreateNotePayload(note),
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// پس از حذف یادداشت فراخوانی می‌شود.
        /// </summary>
        public async Task OnNoteDeletedAsync(
            string noteId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
            {
                return;
            }

            await _tracker.TrackDeleteAsync(
                CloudSyncEntityType.StudyNote,
                noteId,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private static object CreateNotePayload(StudyNote note)
        {
            return new
            {
                note.Id,
                note.Title,
                note.Content,
                note.Tags,
                note.IsImportant,
                note.IsPinned,
                note.BookId,
                note.BookTitle,
                note.LessonId,
                note.LessonTitle,
                note.CreatedAt,
                note.UpdatedAt
            };
        }
    }

    /// <summary>
    /// آداپتور اتصال Cloud Sync به رویدادهای پیشرفت درس.
    /// این کلاس به SqliteDataService متصل می‌شود.
    /// </summary>
    public sealed class CloudSyncProgressAdapter
    {
        private readonly ICloudSyncEventTracker _tracker;

        public CloudSyncProgressAdapter(
            ICloudSyncEventTracker tracker)
        {
            _tracker = tracker ??
                throw new ArgumentNullException(nameof(tracker));
        }

        /// <summary>
        /// پس از ثبت پیشرفت درس فراخوانی می‌شود.
        /// </summary>
        public async Task OnLessonProgressSavedAsync(
            int lessonId,
            bool isCompleted,
            int score,
            string userId,
            CancellationToken cancellationToken = default)
        {
            string entityId = $"{userId}:{lessonId}";

            var payload = new
            {
                LessonId = lessonId,
                IsCompleted = isCompleted,
                Score = score,
                UserId = userId,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            await _tracker.TrackUpdateAsync(
                CloudSyncEntityType.LessonProgress,
                entityId,
                payload,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// آداپتور اتصال Cloud Sync به رویدادهای گواهی.
    /// </summary>
    public sealed class CloudSyncCertificateAdapter
    {
        private readonly ICloudSyncEventTracker _tracker;

        public CloudSyncCertificateAdapter(
            ICloudSyncEventTracker tracker)
        {
            _tracker = tracker ??
                throw new ArgumentNullException(nameof(tracker));
        }

        /// <summary>
        /// پس از صدور گواهی فراخوانی می‌شود.
        /// </summary>
        public async Task OnCertificateIssuedAsync(
            string bookId,
            string bookTitle,
            string certificateCode,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                BookId = bookId,
                BookTitle = bookTitle,
                CertificateCode = certificateCode,
                FilePath = filePath,
                IssuedAtUtc = DateTimeOffset.UtcNow
            };

            await _tracker.TrackCreateAsync(
                CloudSyncEntityType.Certificate,
                certificateCode,
                payload,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// کارخانه ساخت رصدکننده رویدادها.
    /// این کلاس اتصال‌های پیش‌فرض را فراهم می‌کند.
    /// </summary>
    public static class CloudSyncEventTrackerFactory
    {
        /// <summary>
        /// ساخت رصدکننده با سرویس‌های پیش‌فرض.
        /// </summary>
        public static CloudSyncEventTracker CreateDefault(
            string applicationDataDirectory,
            IPremiumAccessService premiumAccessService)
        {
            if (premiumAccessService is null)
            {
                throw new ArgumentNullException(
                    nameof(premiumAccessService));
            }

            var queue = new EncryptedCloudSyncQueue(
                applicationDataDirectory);

            var service = new OfflineFirstCloudSyncService(
                queue,
                () => premiumAccessService.CanAccess(
                    PremiumFeature.CloudSync,
                    forceRefresh: false),
                () => false);

            return new CloudSyncEventTracker(
                service,
                premiumAccessService);
        }
    }
}



