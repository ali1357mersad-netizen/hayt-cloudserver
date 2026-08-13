using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hayt.Cloud.Models;
using System.Net.Http;
using System.IO;



namespace Hayt.Cloud.Services;

/// <summary>
/// دریافت، ذخیره امن و اعمال JWT روی درخواست‌های HTTP.
/// </summary>
public sealed class AuthManager
{
    private static readonly byte[] Entropy =
        Encoding.UTF8.GetBytes("Hayt.Cloud.Auth.Token.v1");

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _tokenFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc;

    public AuthManager(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));

        var applicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(applicationData))
        {
            throw new InvalidOperationException(
                "LocalApplicationData directory is not available.");
        }

        var authDirectory = Path.Combine(
            applicationData,
            "Hayt",
            "Cloud",
            "Auth");

        Directory.CreateDirectory(authDirectory);

        _tokenFilePath = Path.Combine(
            authDirectory,
            "token.dat");

        _jsonOptions = new JsonSerializerOptions(
            JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        LoadToken();
    }

    /// <summary>
    /// مشخص می‌کند توکن ذخیره‌شده همچنان قابل استفاده است یا خیر.
    /// </summary>
    public bool HasUsableToken =>
        !string.IsNullOrWhiteSpace(_accessToken) &&
        _expiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1);

    /// <summary>
    /// زمان انقضای توکن فعلی.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc => _expiresAtUtc;

    /// <summary>
    /// ورود به سرور و دریافت JWT.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(
        string userId,
        string deviceId,
        string loginKey,
        CancellationToken cancellationToken = default)
    {
        userId = NormalizeRequired(
            userId,
            nameof(userId));

        deviceId = NormalizeRequired(
            deviceId,
            nameof(deviceId));

        loginKey = NormalizeRequired(
            loginKey,
            nameof(loginKey));

        var request = new LoginRequest
        {
            UserId = userId,
            DeviceId = deviceId,
            LoginKey = loginKey
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "The server rejected the login credentials.");
        }

        response.EnsureSuccessStatusCode();

        var loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponse>(
                _jsonOptions,
                cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException(
                "The login response was empty or invalid.");

        if (string.IsNullOrWhiteSpace(loginResponse.AccessToken))
        {
            throw new InvalidDataException(
                "The server did not return an access token.");
        }

        if (loginResponse.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new InvalidDataException(
                "The server returned an expired access token.");
        }

        await SetTokenAsync(
            loginResponse.AccessToken,
            loginResponse.ExpiresAtUtc,
            cancellationToken).ConfigureAwait(false);

        return loginResponse;
    }

    /// <summary>
    /// ذخیره امن توکن و اعمال آن روی HttpClient.
    /// </summary>
    public async Task SetTokenAsync(
        string accessToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException(
                "Access token is required.",
                nameof(accessToken));
        }

        if (expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                "Token expiration must be in the future.");
        }

        await _gate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            _accessToken = accessToken.Trim();
            _expiresAtUtc = expiresAtUtc;

            SaveToken();
            ApplyAuthorizationHeader();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// اعمال توکن فعلی روی DefaultRequestHeaders کلاینت HTTP.
    /// </summary>
    public void ApplyAuthorizationHeader()
    {
        if (!HasUsableToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _accessToken);
    }

    /// <summary>
    /// حذف توکن و خروج از حساب Cloud.
    /// </summary>
    public async Task LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            _accessToken = null;
            _expiresAtUtc = default;

            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// اعمال توکن روی یک درخواست مستقل.
    /// در صورت نبودن توکن معتبر false برمی‌گرداند.
    /// </summary>
    public bool TryApplyAuthorizationHeader(
        HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!HasUsableToken)
        {
            request.Headers.Authorization = null;
            return false;
        }

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _accessToken);

        return true;
    }

    /// <summary>
    /// توکن ذخیره‌شده را با DPAPI بارگذاری می‌کند.
    /// </summary>
    private void LoadToken()
    {
        try
        {
            if (!File.Exists(_tokenFilePath))
            {
                return;
            }

            var protectedBytes =
                File.ReadAllBytes(_tokenFilePath);

            var clearBytes = ProtectedData.Unprotect(
                protectedBytes,
                Entropy,
                DataProtectionScope.CurrentUser);

            try
            {
                var persisted =
                    JsonSerializer.Deserialize<PersistedToken>(
                        clearBytes,
                        _jsonOptions);

                if (persisted is null ||
                    string.IsNullOrWhiteSpace(persisted.AccessToken) ||
                    persisted.ExpiresAtUtc <=
                        DateTimeOffset.UtcNow.AddMinutes(1))
                {
                    ResetTokenState();
                    DeleteInvalidTokenFile();
                    return;
                }

                _accessToken = persisted.AccessToken;
                _expiresAtUtc = persisted.ExpiresAtUtc;

                ApplyAuthorizationHeader();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(clearBytes);
            }
        }
        catch (CryptographicException)
        {
            ResetTokenState();
            DeleteInvalidTokenFile();
        }
        catch (IOException)
        {
            ResetTokenState();
            DeleteInvalidTokenFile();
        }
        catch (UnauthorizedAccessException)
        {
            ResetTokenState();
            DeleteInvalidTokenFile();
        }
        catch (JsonException)
        {
            ResetTokenState();
            DeleteInvalidTokenFile();
        }
    }

    /// <summary>
    /// توکن را با DPAPI و محدوده کاربر جاری ذخیره می‌کند.
    /// </summary>
    private void SaveToken()
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            throw new InvalidOperationException(
                "There is no token to save.");
        }

        var persisted = new PersistedToken
        {
            AccessToken = _accessToken,
            ExpiresAtUtc = _expiresAtUtc
        };

        var clearBytes = JsonSerializer.SerializeToUtf8Bytes(
            persisted,
            _jsonOptions);

        try
        {
            var protectedBytes = ProtectedData.Protect(
                clearBytes,
                Entropy,
                DataProtectionScope.CurrentUser);

            try
            {
                var temporaryPath =
                    _tokenFilePath + ".tmp";

                File.WriteAllBytes(
                    temporaryPath,
                    protectedBytes);

                File.Move(
                    temporaryPath,
                    _tokenFilePath,
                    overwrite: true);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(
                    protectedBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clearBytes);
        }
    }

    /// <summary>
    /// وضعیت توکن را از حافظه پاک می‌کند.
    /// </summary>
    private void ResetTokenState()
    {
        _accessToken = null;
        _expiresAtUtc = default;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// فایل توکن خراب یا منقضی‌شده را حذف می‌کند.
    /// </summary>
    private void DeleteInvalidTokenFile()
    {
        try
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }

            var temporaryPath =
                _tokenFilePath + ".tmp";

            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch
        {
            // پاک‌نشدن فایل خراب نباید اجرای برنامه را متوقف کند.
        }
    }

    /// <summary>
    /// مقدار ورودی اجباری را اعتبارسنجی و پاک‌سازی می‌کند.
    /// </summary>
    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A non-empty value is required.",
                parameterName);
        }

        return value.Trim();
    }

    /// <summary>
    /// مدل داخلی ذخیره توکن.
    /// </summary>
    private sealed class PersistedToken
    {
        public string AccessToken { get; init; } =
            string.Empty;

        public DateTimeOffset ExpiresAtUtc { get; init; }
    }
}
