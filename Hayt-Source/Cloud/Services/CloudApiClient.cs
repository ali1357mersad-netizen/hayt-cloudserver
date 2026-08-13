using Hayt.Cloud.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Cloud.Services
{
    public sealed class CloudApiClient : IDisposable
    {
        private const string ApiKeyHeaderName = "X-Hayt-Api-Key";

        private readonly HttpClient _httpClient;
        private readonly CloudClientOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public CloudApiClient(CloudClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new ArgumentException("BaseUrl نمی‌تواند خالی باشد.", nameof(options));
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds))
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
                _httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, _options.ApiKey);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        public async Task<CloudOperationResult<ServerHealthResponse>> GetHealthAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(
                    async token =>
                    {
                        using HttpResponseMessage response =
                            await _httpClient.GetAsync("api/health", token)
                                .ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            return CloudOperationResult<ServerHealthResponse>.Fail(
                                "سرور پاسخ سلامت معتبر نداد. StatusCode=" + response.StatusCode);
                        }

                        ServerHealthResponse? data =
                            await response.Content.ReadFromJsonAsync<ServerHealthResponse>(
                                    _jsonOptions,
                                    token)
                                .ConfigureAwait(false);

                        if (data == null)
                        {
                            return CloudOperationResult<ServerHealthResponse>.Fail(
                                "پاسخ سلامت سرور خالی است.");
                        }

                        return CloudOperationResult<ServerHealthResponse>.Ok(
                            data,
                            "سرور در دسترس است.");
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<CloudSyncPushResponse>> PushAsync(
            CloudSyncPushRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return CloudOperationResult<CloudSyncPushResponse>.Fail(
                    "درخواست Push خالی است.");
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return CloudOperationResult<CloudSyncPushResponse>.Fail(
                    "UserId خالی است.");
            }

            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return CloudOperationResult<CloudSyncPushResponse>.Fail(
                    "DeviceId خالی است.");
            }

            request.Items ??= new List<CloudSyncItem>();

            return await ExecuteWithRetryAsync(
                    async token =>
                    {
                        using HttpResponseMessage response =
                            await _httpClient.PostAsJsonAsync(
                                    "api/cloudsync/push",
                                    request,
                                    _jsonOptions,
                                    token)
                                .ConfigureAwait(false);

                        CloudSyncPushResponse? data =
                            await response.Content.ReadFromJsonAsync<CloudSyncPushResponse>(
                                    _jsonOptions,
                                    token)
                                .ConfigureAwait(false);

                        if (data == null)
                        {
                            return CloudOperationResult<CloudSyncPushResponse>.Fail(
                                "پاسخ Push از سرور خالی است.");
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            data.Success = false;

                            if (string.IsNullOrWhiteSpace(data.Message))
                            {
                                data.Message = "Push ناموفق بود. StatusCode=" + response.StatusCode;
                            }

                            return CloudOperationResult<CloudSyncPushResponse>.Fail(data.Message);
                        }

                        return CloudOperationResult<CloudSyncPushResponse>.Ok(
                            data,
                            data.Message);
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<CloudSyncPullResponse>> PullAsync(
            CloudSyncPullRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return CloudOperationResult<CloudSyncPullResponse>.Fail(
                    "درخواست Pull خالی است.");
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return CloudOperationResult<CloudSyncPullResponse>.Fail(
                    "UserId خالی است.");
            }

            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return CloudOperationResult<CloudSyncPullResponse>.Fail(
                    "DeviceId خالی است.");
            }

            return await ExecuteWithRetryAsync(
                    async token =>
                    {
                        using HttpResponseMessage response =
                            await _httpClient.PostAsJsonAsync(
                                    "api/cloudsync/pull",
                                    request,
                                    _jsonOptions,
                                    token)
                                .ConfigureAwait(false);

                        CloudSyncPullResponse? data =
                            await response.Content.ReadFromJsonAsync<CloudSyncPullResponse>(
                                    _jsonOptions,
                                    token)
                                .ConfigureAwait(false);

                        if (data == null)
                        {
                            return CloudOperationResult<CloudSyncPullResponse>.Fail(
                                "پاسخ Pull از سرور خالی است.");
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            data.Success = false;

                            if (string.IsNullOrWhiteSpace(data.Message))
                            {
                                data.Message = "Pull ناموفق بود. StatusCode=" + response.StatusCode;
                            }

                            return CloudOperationResult<CloudSyncPullResponse>.Fail(data.Message);
                        }

                        return CloudOperationResult<CloudSyncPullResponse>.Ok(
                            data,
                            data.Message);
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CloudOperationResult<List<OnlineUserDto>>> GetOnlineUsersAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(
                    async token =>
                    {
                        using HttpResponseMessage response =
                            await _httpClient.GetAsync("api/online/users", token)
                                .ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            return CloudOperationResult<List<OnlineUserDto>>.Fail(
                                "دریافت کاربران آنلاین ناموفق بود. StatusCode=" + response.StatusCode);
                        }

                        List<OnlineUserDto>? data =
                            await response.Content.ReadFromJsonAsync<List<OnlineUserDto>>(
                                    _jsonOptions,
                                    token)
                                .ConfigureAwait(false);

                        data ??= new List<OnlineUserDto>();

                        return CloudOperationResult<List<OnlineUserDto>>.Ok(
                            data,
                            "کاربران آنلاین دریافت شدند.");
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<CloudOperationResult<T>> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<CloudOperationResult<T>>> operation,
            CancellationToken cancellationToken)
        {
            int retryCount = Math.Max(1, _options.MaxRetryCount);
            int delay = Math.Max(100, _options.RetryDelayMilliseconds);

            Exception? lastException = null;
            CloudOperationResult<T>? lastResult = null;

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    CloudOperationResult<T> result =
                        await operation(cancellationToken).ConfigureAwait(false);

                    if (result.Success)
                    {
                        return result;
                    }

                    lastResult = result;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    lastResult = CloudOperationResult<T>.Fail(
                        "خطا در ارتباط با سرور: " + ex.Message,
                        ex);
                }

                if (attempt < retryCount)
                {
                    await Task.Delay(delay * attempt, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            return lastResult ??
                CloudOperationResult<T>.Fail(
                    "عملیات پس از چند تلاش ناموفق بود.",
                    lastException);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _httpClient.Dispose();
            _disposed = true;
        }
    }
}