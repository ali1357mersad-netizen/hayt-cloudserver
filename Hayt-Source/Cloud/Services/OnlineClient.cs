using Hayt.Cloud.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Hayt.Cloud.Services
{
    public sealed class OnlineClient : IAsyncDisposable
    {
        private readonly CloudClientOptions _options;
        private readonly Dispatcher? _dispatcher;

        private HubConnection? _connection;
        private bool _disposed;

        public OnlineClient(
            CloudClientOptions options,
            Dispatcher? dispatcher = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _dispatcher = dispatcher;
        }

        public bool IsConnected =>
            _connection?.State == HubConnectionState.Connected;

        public event EventHandler<IReadOnlyList<OnlineUserDto>>? OnlineUsersChanged;
        public event EventHandler<OnlineMessageDto>? PublicMessageReceived;
        public event EventHandler<string>? ConnectionStateChanged;

        public async Task<CloudOperationResult> ConnectAsync(
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return CloudOperationResult.Fail("OnlineClient قبلاً Dispose شده است.");
            }

            if (IsConnected)
            {
                return CloudOperationResult.Ok("قبلاً متصل است.");
            }

            try
            {
                string hubUrl =
                    _options.BaseUrl.TrimEnd('/') + "/hubs/online";

                _connection = new HubConnectionBuilder()
                    .WithUrl(
                        hubUrl,
                        options =>
                        {
                            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                            {
                                options.Headers.Add("X-Hayt-Api-Key", _options.ApiKey);
                            }
                        })
                    .WithAutomaticReconnect()
                    .Build();

                RegisterEvents(_connection);

                await RaiseConnectionStateChangedAsync("در حال اتصال...")
                    .ConfigureAwait(false);

                await ExecuteWithRetryAsync(
                        async token =>
                        {
                            await _connection.StartAsync(token)
                                .ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await ExecuteWithRetryAsync(
                        async token =>
                        {
                            await _connection.InvokeAsync(
                                    "JoinOnline",
                                    _options.UserId,
                                    _options.DisplayName,
                                    _options.DeviceId,
                                    token)
                                .ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await RaiseConnectionStateChangedAsync("متصل شد.")
                    .ConfigureAwait(false);

                return CloudOperationResult.Ok("اتصال آنلاین برقرار شد.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await RaiseConnectionStateChangedAsync("اتصال ناموفق بود.")
                    .ConfigureAwait(false);

                return CloudOperationResult.Fail(
                    "خطا در اتصال SignalR: " + ex.Message,
                    ex);
            }
        }

        public async Task<CloudOperationResult<IReadOnlyList<OnlineUserDto>>> GetOnlineUsersAsync(
            CancellationToken cancellationToken = default)
        {
            if (_connection == null || !IsConnected)
            {
                return CloudOperationResult<IReadOnlyList<OnlineUserDto>>.Fail(
                    "SignalR متصل نیست.");
            }

            try
            {
                List<OnlineUserDto> users =
                    await ExecuteWithRetryAsync(
                            async token =>
                            {
                                return await _connection.InvokeAsync<List<OnlineUserDto>>(
                                        "GetOnlineUsers",
                                        token)
                                    .ConfigureAwait(false);
                            },
                            cancellationToken)
                        .ConfigureAwait(false);

                return CloudOperationResult<IReadOnlyList<OnlineUserDto>>.Ok(
                    users,
                    "کاربران آنلاین دریافت شدند.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return CloudOperationResult<IReadOnlyList<OnlineUserDto>>.Fail(
                    "خطا در دریافت کاربران آنلاین: " + ex.Message,
                    ex);
            }
        }

        public async Task<CloudOperationResult> SendPublicMessageAsync(
            string message,
            CancellationToken cancellationToken = default)
        {
            if (_connection == null || !IsConnected)
            {
                return CloudOperationResult.Fail("SignalR متصل نیست.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return CloudOperationResult.Fail("متن پیام خالی است.");
            }

            try
            {
                await ExecuteWithRetryAsync(
                        async token =>
                        {
                            await _connection.InvokeAsync(
                                    "SendPublicMessage",
                                    _options.UserId,
                                    _options.DisplayName,
                                    message,
                                    token)
                                .ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                return CloudOperationResult.Ok("پیام ارسال شد.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return CloudOperationResult.Fail(
                    "خطا در ارسال پیام: " + ex.Message,
                    ex);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection == null)
            {
                return;
            }

            try
            {
                await RaiseConnectionStateChangedAsync("در حال قطع اتصال...")
                    .ConfigureAwait(false);

                await _connection.StopAsync()
                    .ConfigureAwait(false);

                await _connection.DisposeAsync()
                    .ConfigureAwait(false);

                await RaiseConnectionStateChangedAsync("قطع اتصال شد.")
                    .ConfigureAwait(false);
            }
            finally
            {
                _connection = null;
            }
        }

        private void RegisterEvents(HubConnection connection)
        {
            connection.On<List<OnlineUserDto>>(
                "OnlineUsersChanged",
                users =>
                {
                    _ = RunOnUiAsync(() =>
                    {
                        OnlineUsersChanged?.Invoke(this, users);
                    });
                });

            connection.On<OnlineMessageDto>(
                "PublicMessageReceived",
                message =>
                {
                    _ = RunOnUiAsync(() =>
                    {
                        PublicMessageReceived?.Invoke(this, message);
                    });
                });

            connection.Reconnecting += async _ =>
            {
                await RaiseConnectionStateChangedAsync("در حال اتصال مجدد...")
                    .ConfigureAwait(false);
            };

            connection.Reconnected += async _ =>
            {
                await RaiseConnectionStateChangedAsync("اتصال مجدد برقرار شد.")
                    .ConfigureAwait(false);

                try
                {
                    await connection.InvokeAsync(
                            "JoinOnline",
                            _options.UserId,
                            _options.DisplayName,
                            _options.DeviceId)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // خطای Join مجدد نباید برنامه را خراب کند.
                }
            };

            connection.Closed += async _ =>
            {
                await RaiseConnectionStateChangedAsync("اتصال بسته شد.")
                    .ConfigureAwait(false);
            };
        }

        private async Task ExecuteWithRetryAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            int retryCount = Math.Max(1, _options.MaxRetryCount);
            int delay = Math.Max(100, _options.RetryDelayMilliseconds);

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    await action(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch when (attempt < retryCount)
                {
                    await Task.Delay(delay * attempt, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            await action(cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken)
        {
            int retryCount = Math.Max(1, _options.MaxRetryCount);
            int delay = Math.Max(100, _options.RetryDelayMilliseconds);

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    return await action(cancellationToken).ConfigureAwait(false);
                }
                catch when (attempt < retryCount)
                {
                    await Task.Delay(delay * attempt, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            return await action(cancellationToken).ConfigureAwait(false);
        }

        private async Task RunOnUiAsync(Action action)
        {
            if (_dispatcher == null || _dispatcher.CheckAccess())
            {
                action();
                return;
            }

            await _dispatcher.InvokeAsync(action);
        }

        private async Task RaiseConnectionStateChangedAsync(string state)
        {
            await RunOnUiAsync(() =>
            {
                ConnectionStateChanged?.Invoke(this, state);
            }).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await DisconnectAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }
}