using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// وضعیت احراز هویت.
    /// </summary>
    public enum CloudSyncAuthState
    {
        NotAuthenticated = 0,
        Authenticated = 1,
        TokenExpired = 2,
        RefreshRequired = 3,
        Failed = 4
    }

    /// <summary>
    /// نتیجه احراز هویت.
    /// </summary>
    public sealed class CloudSyncAuthResult
    {
        public CloudSyncAuthResult(
            bool isSuccessful,
            CloudSyncAuthState state,
            string message)
        {
            IsSuccessful = isSuccessful;
            State = state;
            Message = message ?? string.Empty;
        }

        public bool IsSuccessful { get; }

        public CloudSyncAuthState State { get; }

        public string Message { get; }
    }

    /// <summary>
    /// داده‌های توکن ذخیره‌شده.
    /// </summary>
    public sealed class CloudSyncTokenData
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset AccessTokenExpiresAtUtc { get; set; }

        public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }

        public string UserId { get; set; } = string.Empty;

        public bool IsAccessTokenValid(DateTimeOffset now)
        {
            return !string.IsNullOrWhiteSpace(AccessToken) &&
                now < AccessTokenExpiresAtUtc;
        }

        public bool IsRefreshTokenValid(DateTimeOffset now)
        {
            return !string.IsNullOrWhiteSpace(RefreshToken) &&
                now < RefreshTokenExpiresAtUtc;
        }
    }

    /// <summary>
    /// مدیر احراز هویت و توکن Cloud Sync.
    /// توکن‌ها با DPAPI ویندوز محافظت می‌شوند.
    /// </summary>
    public sealed class CloudSyncAuthManager
    {
        private const int TokenFileVersion = 1;

        private readonly string _tokenFilePath;
        private readonly SemaphoreSlim _gate =
            new SemaphoreSlim(1, 1);

        private CloudSyncTokenData? _cachedToken;

        public CloudSyncAuthManager(
            string applicationDataDirectory)
        {
            if (string.IsNullOrWhiteSpace(applicationDataDirectory))
            {
                throw new ArgumentException(
                    "مسیر ذخیره‌سازی نمی‌تواند خالی باشد.",
                    nameof(applicationDataDirectory));
            }

            string syncDirectory = Path.Combine(
                applicationDataDirectory,
                "CloudSync");

            Directory.CreateDirectory(syncDirectory);

            _tokenFilePath = Path.Combine(
                syncDirectory,
                "auth.token");
        }

        /// <summary>
        /// وضعیت فعلی احراز هویت.
        /// </summary>
        public CloudSyncAuthState State
        {
            get
            {
                CloudSyncTokenData? token =
                    GetCachedToken();

                if (token is null)
                {
                    return CloudSyncAuthState.NotAuthenticated;
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;

                if (token.IsAccessTokenValid(now))
                {
                    return CloudSyncAuthState.Authenticated;
                }

                if (token.IsRefreshTokenValid(now))
                {
                    return CloudSyncAuthState.RefreshRequired;
                }

                return CloudSyncAuthState.TokenExpired;
            }
        }

        /// <summary>
        /// ذخیره توکن جدید.
        /// </summary>
        public async Task<CloudSyncAuthResult> StoreTokenAsync(
            CloudSyncTokenData token,
            CancellationToken cancellationToken = default)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (string.IsNullOrWhiteSpace(token.AccessToken))
            {
                return new CloudSyncAuthResult(
                    false,
                    CloudSyncAuthState.Failed,
                    "توکن دسترسی نمی‌تواند خالی باشد.");
            }

            if (string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                return new CloudSyncAuthResult(
                    false,
                    CloudSyncAuthState.Failed,
                    "توکن تازه‌سازی نمی‌تواند خالی باشد.");
            }

            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                byte[] plainText =
                    JsonSerializer.SerializeToUtf8Bytes(token);

                byte[] protectedData =
                    ProtectedData.Protect(
                        plainText,
                        optionalEntropy: null,
                        DataProtectionScope.CurrentUser);

                byte[] package = new byte[
                    1 + protectedData.Length];

                package[0] = TokenFileVersion;
                Buffer.BlockCopy(
                    protectedData,
                    0,
                    package,
                    1,
                    protectedData.Length);

                await WriteAtomicallyAsync(
                        package,
                        cancellationToken)
                    .ConfigureAwait(false);

                _cachedToken = token;

                return new CloudSyncAuthResult(
                    true,
                    CloudSyncAuthState.Authenticated,
                    "توکن با موفقیت ذخیره شد.");
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// دریافت توکن دسترسی معتبر.
        /// </summary>
        public async Task<CloudSyncAuthResult> GetAccessTokenAsync(
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                CloudSyncTokenData? token =
                    await ReadTokenInternalAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (token is null)
                {
                    return new CloudSyncAuthResult(
                        false,
                        CloudSyncAuthState.NotAuthenticated,
                        "هیچ توکنی ذخیره نشده است.");
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;

                if (token.IsAccessTokenValid(now))
                {
                    _cachedToken = token;

                    return new CloudSyncAuthResult(
                        true,
                        CloudSyncAuthState.Authenticated,
                        "توکن دسترسی معتبر است.");
                }

                if (token.IsRefreshTokenValid(now))
                {
                    _cachedToken = token;

                    return new CloudSyncAuthResult(
                        false,
                        CloudSyncAuthState.RefreshRequired,
                        "توکن دسترسی منقضی شده؛ تازه‌سازی لازم است.");
                }

                return new CloudSyncAuthResult(
                    false,
                    CloudSyncAuthState.TokenExpired,
                    "توکن‌ها منقضی شده‌اند؛ ورود مجدد لازم است.");
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// تازه‌سازی توکن با توکن تازه‌سازی.
        /// </summary>
        public async Task<CloudSyncAuthResult> RefreshTokenAsync(
            Func<CloudSyncTokenData, Task<CloudSyncTokenData>> refreshCallback,
            CancellationToken cancellationToken = default)
        {
            if (refreshCallback is null)
            {
                throw new ArgumentNullException(
                    nameof(refreshCallback));
            }

            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                CloudSyncTokenData? token =
                    await ReadTokenInternalAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (token is null)
                {
                    return new CloudSyncAuthResult(
                        false,
                        CloudSyncAuthState.NotAuthenticated,
                        "هیچ توکنی برای تازه‌سازی وجود ندارد.");
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;

                if (!token.IsRefreshTokenValid(now))
                {
                    return new CloudSyncAuthResult(
                        false,
                        CloudSyncAuthState.TokenExpired,
                        "توکن تازه‌سازی منقضی شده؛ ورود مجدد لازم است.");
                }

                CloudSyncTokenData refreshed =
                    await refreshCallback(token)
                        .ConfigureAwait(false);

                if (refreshed is null ||
                    string.IsNullOrWhiteSpace(refreshed.AccessToken))
                {
                    return new CloudSyncAuthResult(
                        false,
                        CloudSyncAuthState.Failed,
                        "پاسخ تازه‌سازی معتبر نیست.");
                }

                refreshed.UserId = token.UserId;

                byte[] plainText =
                    JsonSerializer.SerializeToUtf8Bytes(refreshed);

                byte[] protectedData =
                    ProtectedData.Protect(
                        plainText,
                        optionalEntropy: null,
                        DataProtectionScope.CurrentUser);

                byte[] package = new byte[
                    1 + protectedData.Length];

                package[0] = TokenFileVersion;
                Buffer.BlockCopy(
                    protectedData,
                    0,
                    package,
                    1,
                    protectedData.Length);

                await WriteAtomicallyAsync(
                        package,
                        cancellationToken)
                    .ConfigureAwait(false);

                _cachedToken = refreshed;

                return new CloudSyncAuthResult(
                    true,
                    CloudSyncAuthState.Authenticated,
                    "توکن با موفقیت تازه‌سازی شد.");
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// پاک‌سازی توکن (خروج از حساب).
        /// </summary>
        public async Task ClearTokenAsync(
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                if (File.Exists(_tokenFilePath))
                {
                    File.Delete(_tokenFilePath);
                }

                _cachedToken = null;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// ساخت هدر Authorization.
        /// </summary>
        public async Task<string?> BuildAuthorizationHeaderAsync(
            CancellationToken cancellationToken = default)
        {
            CloudSyncAuthResult result =
                await GetAccessTokenAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (!result.IsSuccessful)
            {
                return null;
            }

            CloudSyncTokenData? token =
                await ReadTokenInternalAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (token is null ||
                string.IsNullOrWhiteSpace(token.AccessToken))
            {
                return null;
            }

            return "Bearer " + token.AccessToken;
        }

        private CloudSyncTokenData? GetCachedToken()
        {
            return _cachedToken;
        }

        private async Task<CloudSyncTokenData?> ReadTokenInternalAsync(
            CancellationToken cancellationToken)
        {
            if (!File.Exists(_tokenFilePath))
            {
                return null;
            }

            byte[] package =
                await File.ReadAllBytesAsync(
                        _tokenFilePath,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (package.Length < 2)
            {
                throw new InvalidDataException(
                    "فایل توکن Cloud Sync معتبر نیست.");
            }

            int version = package[0];

            if (version != TokenFileVersion)
            {
                throw new InvalidDataException(
                    "نسخه فایل توکن Cloud Sync پشتیبانی نمی‌شود.");
            }

            byte[] protectedData =
                new byte[package.Length - 1];

            Buffer.BlockCopy(
                package,
                1,
                protectedData,
                0,
                protectedData.Length);

            byte[] plainText =
                ProtectedData.Unprotect(
                    protectedData,
                    optionalEntropy: null,
                    DataProtectionScope.CurrentUser);

            CloudSyncTokenData? token =
                JsonSerializer.Deserialize<CloudSyncTokenData>(
                    plainText);

            return token;
        }

        private async Task WriteAtomicallyAsync(
            byte[] data,
            CancellationToken cancellationToken)
        {
            string temporaryPath =
                _tokenFilePath + ".tmp";

            try
            {
                await File.WriteAllBytesAsync(
                        temporaryPath,
                        data,
                        cancellationToken)
                    .ConfigureAwait(false);

                File.Move(
                    temporaryPath,
                    _tokenFilePath,
                    true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}