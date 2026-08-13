using System;
using System.IO;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// کارخانه ساخت سرویس Cloud Sync.
    /// </summary>
    public static class CloudSyncServiceFactory
    {
        public static ICloudSyncService Create(
            string applicationDataDirectory,
            Func<bool> hasPremiumAccess,
            Func<bool> isOnline)
        {
            var queue = new EncryptedCloudSyncQueue(
                applicationDataDirectory);

            return new OfflineFirstCloudSyncService(
                queue,
                hasPremiumAccess,
                isOnline);
        }
    }
}
