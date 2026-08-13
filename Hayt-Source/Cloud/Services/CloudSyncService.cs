using Hayt.Cloud.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Cloud.Services
{
    public sealed class CloudSyncService
    {
        private readonly CloudApiClient _apiClient;
        private readonly CloudClientOptions _options;

        public CloudSyncService(
            CloudApiClient apiClient,
            CloudClientOptions options)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public event Func<CloudSyncItem, Task>? RemoteItemReceivedAsync;

        public async Task<CloudOperationResult<ServerHealthResponse>> CheckHealthAsync(
            CancellationToken cancellationToken = default)
        {
            return await _apiClient.GetHealthAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<CloudSyncPushResponse>> PushAsync(
            IEnumerable<CloudSyncItem> items,
            CancellationToken cancellationToken = default)
        {
            List<CloudSyncItem> safeItems =
                items?
                    .Where(IsValidItem)
                    .ToList()
                ?? new List<CloudSyncItem>();

            CloudSyncPushRequest request = new()
            {
                UserId = _options.UserId,
                DeviceId = _options.DeviceId,
                Items = safeItems
            };

            return await _apiClient.PushAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<CloudSyncPullResponse>> PullAsync(
            DateTimeOffset? sinceUtc,
            CancellationToken cancellationToken = default)
        {
            CloudSyncPullRequest request = new()
            {
                UserId = _options.UserId,
                DeviceId = _options.DeviceId,
                SinceUtc = sinceUtc
            };

            return await _apiClient.PullAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<CloudSyncPullResponse>> SyncAsync(
            IEnumerable<CloudSyncItem> localPendingItems,
            DateTimeOffset? sinceUtc,
            CancellationToken cancellationToken = default)
        {
            CloudOperationResult<CloudSyncPushResponse> pushResult =
                await PushAsync(localPendingItems, cancellationToken)
                    .ConfigureAwait(false);

            if (!pushResult.Success)
            {
                return CloudOperationResult<CloudSyncPullResponse>.Fail(
                    "ارسال تغییرات محلی ناموفق بود: " + pushResult.Message,
                    pushResult.Exception);
            }

            CloudOperationResult<CloudSyncPullResponse> pullResult =
                await PullAsync(sinceUtc, cancellationToken)
                    .ConfigureAwait(false);

            if (!pullResult.Success || pullResult.Data == null)
            {
                return pullResult;
            }

            if (RemoteItemReceivedAsync != null)
            {
                foreach (CloudSyncItem item in pullResult.Data.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await RemoteItemReceivedAsync.Invoke(item)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // مدیریت تعارض در لایه بالاتر
                    }
                }
            }

            return pullResult;
        }

        private static bool IsValidItem(CloudSyncItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.Id == Guid.Empty)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.EntityType))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.EntityId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.OperationType))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.PayloadJson))
            {
                item.PayloadJson = "{}";
            }

            return true;
        }
    }
}