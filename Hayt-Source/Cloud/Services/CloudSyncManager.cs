using Hayt.Cloud.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Hayt.Cloud.Services
{
    public sealed class CloudSyncManager : IAsyncDisposable
    {
        private readonly CloudClientOptions _options;
        private readonly Dispatcher _dispatcher;
        private readonly CloudApiClient _apiClient;
        private readonly CloudSyncService _syncService;
        private readonly OnlineClient _onlineClient;

        private readonly SemaphoreSlim _syncGate = new(1, 1);
        private bool _disposed;

        public CloudSyncManager(
            CloudClientOptions options,
            Dispatcher dispatcher)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            _apiClient = new CloudApiClient(_options);
            _syncService = new CloudSyncService(_apiClient, _options);
            _onlineClient = new OnlineClient(_options, _dispatcher);

            _onlineClient.OnlineUsersChanged += (_, users) =>
            {
                OnlineUsersChanged?.Invoke(this, users);
            };

            _onlineClient.PublicMessageReceived += (_, message) =>
            {
                PublicMessageReceived?.Invoke(this, message);
            };

            _onlineClient.ConnectionStateChanged += (_, state) =>
            {
                StatusMessageChanged?.Invoke(this, state);
            };

            _syncService.RemoteItemReceivedAsync += async item =>
            {
                if (RemoteItemReceivedAsync != null)
                {
                    await RemoteItemReceivedAsync.Invoke(item)
                        .ConfigureAwait(false);
                }
            };
        }

        public CloudConnectionState State { get; private set; } =
            CloudConnectionState.Disconnected;

        public DateTimeOffset? LastSuccessfulSyncUtc { get; private set; }

        public event EventHandler<CloudConnectionState>? StateChanged;
        public event EventHandler<string>? StatusMessageChanged;
        public event EventHandler<IReadOnlyList<OnlineUserDto>>? OnlineUsersChanged;
        public event EventHandler<OnlineMessageDto>? PublicMessageReceived;
        public event Func<CloudSyncItem, Task>? RemoteItemReceivedAsync;

        public async Task<CloudOperationResult<ServerHealthResponse>> CheckServerAsync(
            CancellationToken cancellationToken = default)
        {
            CloudOperationResult<ServerHealthResponse> result =
                await _apiClient.GetHealthAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (result.Success)
            {
                await SetStateAsync(
                        CloudConnectionState.Connected,
                        "سرور Cloud در دسترس است.")
                    .ConfigureAwait(false);
            }
            else
            {
                await SetStateAsync(
                        CloudConnectionState.Failed,
                        result.Message)
                    .ConfigureAwait(false);
            }

            return result;
        }

        public async Task<CloudOperationResult> ConnectOnlineAsync(
            CancellationToken cancellationToken = default)
        {
            await SetStateAsync(
                    CloudConnectionState.Connecting,
                    "در حال اتصال به بخش آنلاین...")
                .ConfigureAwait(false);

            CloudOperationResult result =
                await _onlineClient.ConnectAsync(cancellationToken)
                    .ConfigureAwait(false);

            await SetStateAsync(
                    result.Success ? CloudConnectionState.Connected : CloudConnectionState.Failed,
                    result.Message)
                .ConfigureAwait(false);

            return result;
        }

        public async Task DisconnectOnlineAsync()
        {
            await _onlineClient.DisconnectAsync()
                .ConfigureAwait(false);

            await SetStateAsync(
                    CloudConnectionState.Disconnected,
                    "اتصال آنلاین قطع شد.")
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult> SendPublicMessageAsync(
            string message,
            CancellationToken cancellationToken = default)
        {
            return await _onlineClient.SendPublicMessageAsync(message, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<IReadOnlyList<OnlineUserDto>>> GetOnlineUsersAsync(
            CancellationToken cancellationToken = default)
        {
            return await _onlineClient.GetOnlineUsersAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<CloudSyncPullResponse>> SynchronizeAsync(
            IEnumerable<CloudSyncItem> localPendingItems,
            DateTimeOffset? sinceUtc,
            CancellationToken cancellationToken = default)
        {
            await _syncGate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await SetStateAsync(
                        CloudConnectionState.Synchronizing,
                        "در حال همگام‌سازی...")
                    .ConfigureAwait(false);

                CloudOperationResult<CloudSyncPullResponse> result =
                    await _syncService.SyncAsync(
                            localPendingItems,
                            sinceUtc,
                            cancellationToken)
                        .ConfigureAwait(false);

                if (result.Success)
                {
                    LastSuccessfulSyncUtc =
                        result.Data?.ServerTimeUtc ?? DateTimeOffset.UtcNow;

                    await SetStateAsync(
                            CloudConnectionState.Connected,
                            "همگام‌سازی با موفقیت انجام شد.")
                        .ConfigureAwait(false);
                }
                else
                {
                    await SetStateAsync(
                            CloudConnectionState.Failed,
                            result.Message)
                        .ConfigureAwait(false);
                }

                return result;
            }
            finally
            {
                _syncGate.Release();
            }
        }

        public async Task<CloudOperationResult<CloudSyncPushResponse>> PushOnlyAsync(
            IEnumerable<CloudSyncItem> localPendingItems,
            CancellationToken cancellationToken = default)
        {
            await _syncGate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await SetStateAsync(
                        CloudConnectionState.Synchronizing,
                        "در حال ارسال تغییرات...")
                    .ConfigureAwait(false);

                CloudOperationResult<CloudSyncPushResponse> result =
                    await _syncService.PushAsync(localPendingItems, cancellationToken)
                        .ConfigureAwait(false);

                await SetStateAsync(
                        result.Success ? CloudConnectionState.Connected : CloudConnectionState.Failed,
                        result.Message)
                    .ConfigureAwait(false);

                return result;
            }
            finally
            {
                _syncGate.Release();
            }
        }

        public async Task<CloudOperationResult<CloudSyncPullResponse>> PullOnlyAsync(
            DateTimeOffset? sinceUtc,
            CancellationToken cancellationToken = default)
        {
            await _syncGate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await SetStateAsync(
                        CloudConnectionState.Synchronizing,
                        "در حال دریافت تغییرات...")
                    .ConfigureAwait(false);

                CloudOperationResult<CloudSyncPullResponse> result =
                    await _syncService.PullAsync(sinceUtc, cancellationToken)
                        .ConfigureAwait(false);

                await SetStateAsync(
                        result.Success ? CloudConnectionState.Connected : CloudConnectionState.Failed,
                        result.Message)
                    .ConfigureAwait(false);

                return result;
            }
            finally
            {
                _syncGate.Release();
            }
        }

        private async Task SetStateAsync(
            CloudConnectionState state,
            string message)
        {
            State = state;

            if (_dispatcher.CheckAccess())
            {
                StateChanged?.Invoke(this, state);
                StatusMessageChanged?.Invoke(this, message);
                return;
            }

            await _dispatcher.InvokeAsync(() =>
            {
                StateChanged?.Invoke(this, state);
                StatusMessageChanged?.Invoke(this, message);
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _onlineClient.DisposeAsync()
                .ConfigureAwait(false);

            _apiClient.Dispose();
            _syncGate.Dispose();

            _disposed = true;
        }
    }
}