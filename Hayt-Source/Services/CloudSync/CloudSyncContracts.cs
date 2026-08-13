using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// قرارداد عمومی سرویس Cloud Sync.
    /// </summary>
    public interface ICloudSyncService
    {
        CloudSyncState State { get; }

        Task EnqueueAsync(
            CloudSyncQueueItem item,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CloudSyncQueueItem>> GetPendingAsync(
            CancellationToken cancellationToken = default);

        Task<CloudSyncResult> SynchronizeAsync(
            CancellationToken cancellationToken = default);
    }
}
