using Hayt.Licensing.Services;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

public sealed class LicenseService : ILicenseService
{
    private readonly object _sync = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /*
     * IMPORTANT:
     * این کلید عمومی نمونه است و برای Build و توسعه قرار داده شده.
     * در مرحله تولید واقعی باید کلید عمومی نهایی پروژه جایگزین شود.
     *
     * فرمت مورد استفاده:
     * PEM SubjectPublicKeyInfo
     */
    private const string PublicKeyPem = """
-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAzpnjX1RoJSm+nHZhBRB/
/xODmorvVrThbrI7GS0htAW6c3AxzKsgxCanNoXdpO3qBb64A8PFF3s6xCyHQdj7
2EfnkXBQv0MqJ7BWqfthZbG5RAKDgrBQGoTC7l8S3mHvHTIkmD2o5Fzt1BcvAFDR
uJLMcJVQVik1CmUcWegt/lrwSGK5xFoNoa6rLfWZMVdoZfwkoEfsfRFW/gaupnOj
vgVrhDg1ntLe8xURm7Q6OEw+WIFNq96NatOlFc24SlJohwyKcRxLrBWOdO5jsM4Z
oZMqi4JSjTASudO7pSKdSjodw0S/pwrK+vCnelY6hCWd6s2JJZalkb00vpwCD3P0
C15y1oepeD3q9TTWMMcFkSoQPOOTlJidY/1evjl19TZ86rxN4kW4lsRWshJXWQXZ
5uYwSYCCQk1HzKOfIQMGPaHEvxSUKUOMfxRkP6pq59YzMsQq21RDr2KlRu5lfJWu
o46gVlm1uSVVq6807suyqXMnUt5qCeubF7a9BXoC23eVAgMBAAE=
-----END PUBLIC KEY-----
""";

    public LicenseState Current { get; private set; } =
        LicenseState.CreateDefault();

    public event EventHandler? LicenseChanged;

    private string AppDirectory
    {
        get
        {
            string appData =
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Path.Combine(appData, "Hayt");
        }
    }

    private string StateFilePath =>
        Path.Combine(AppDirectory, "license-state.json");

    public LicenseState Load()
    {
        lock (_sync)
        {
            try
            {
                if (!Directory.Exists(AppDirectory))
                {
                    Directory.CreateDirectory(AppDirectory);
                }

                if (!File.Exists(StateFilePath))
                {
                    Current = LicenseState.CreateDefault();
                    SaveInternal(Current);
                    LicenseChanged?.Invoke(this, EventArgs.Empty);
                    return Current;
                }

                string json = File.ReadAllText(StateFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Current = LicenseState.CreateDefault();
                    SaveInternal(Current);
                    LicenseChanged?.Invoke(this, EventArgs.Empty);
                    return Current;
                }

                Current = JsonSerializer.Deserialize<LicenseState>(json, _jsonOptions)
                    ?? LicenseState.CreateDefault();

                if (Current.FirstRunAt == default)
                {
                    Current.FirstRunAt = DateTime.UtcNow;
                }

                Current.LastCheckedAt = DateTime.UtcNow;
                SaveInternal(Current);

                LicenseChanged?.Invoke(this, EventArgs.Empty);
                return Current;
            }
            catch
            {
                Current = LicenseState.CreateDefault();
                return Current;
            }
        }
    }

    public LicenseValidationResult ValidateCurrent()
    {
        lock (_sync)
        {
            Load();

            if (!Current.IsActivated ||
                string.IsNullOrWhiteSpace(Current.LicenseJsonBase64) ||
                string.IsNullOrWhiteSpace(Current.SignatureBase64))
            {
                if (Current.IsTrialActive)
                {
                    return LicenseValidationResult.Trial(Current.TrialDaysLeft);
                }

                return LicenseValidationResult.Invalid(
                    "لایسنس فعال نشده و دوره آزمایشی پایان یافته است.");
            }

            try
            {
                byte[] payloadBytes =
                    Convert.FromBase64String(Current.LicenseJsonBase64);

                byte[] signatureBytes =
                    Convert.FromBase64String(Current.SignatureBase64);

                bool signatureValid =
                    VerifySignature(payloadBytes, signatureBytes);

                if (!signatureValid)
                {
                    Current.IsActivated = false;
                    Current.LastError = "امضای دیجیتال لایسنس معتبر نیست.";
                    SaveInternal(Current);

                    return LicenseValidationResult.Invalid(
                        "امضای دیجیتال لایسنس معتبر نیست.");
                }

                string payloadJson =
                    Encoding.UTF8.GetString(payloadBytes);

                LicensePayload? payload =
                    JsonSerializer.Deserialize<LicensePayload>(
                        payloadJson,
                        _jsonOptions);

                if (payload is null)
                {
                    Current.IsActivated = false;
                    Current.LastError = "Payload لایسنس قابل خواندن نیست.";
                    SaveInternal(Current);

                    return LicenseValidationResult.Invalid(
                        "Payload لایسنس قابل خواندن نیست.");
                }

                if (!string.Equals(
                    payload.ProductCode,
                    "Hayt",
                    StringComparison.OrdinalIgnoreCase))
                {
                    Current.IsActivated = false;
                    Current.LastError = "لایسنس برای این محصول صادر نشده است.";
                    SaveInternal(Current);

                    return LicenseValidationResult.Invalid(
                        "لایسنس برای این محصول صادر نشده است.");
                }

                if (payload.IsExpired)
                {
                    Current.IsActivated = false;
                    Current.Payload = payload;
                    Current.LastError = "لایسنس منقضی شده است.";
                    SaveInternal(Current);

                    return LicenseValidationResult.Invalid(
                        "لایسنس منقضی شده است.",
                        isExpired: true);
                }

                if (!string.IsNullOrWhiteSpace(payload.MachineId))
                {
                    string currentMachineId = GetMachineId();

                    if (!string.Equals(
                        payload.MachineId,
                        currentMachineId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        Current.IsActivated = false;
                        Current.Payload = payload;
                        Current.LastError = "لایسنس برای این دستگاه صادر نشده است.";
                        SaveInternal(Current);

                        return LicenseValidationResult.Invalid(
                            "لایسنس برای این دستگاه صادر نشده است.",
                            machineMismatch: true);
                    }
                }

                Current.IsActivated = true;
                Current.Payload = payload;
                Current.LastError = string.Empty;
                Current.LastCheckedAt = DateTime.UtcNow;
                SaveInternal(Current);

                return LicenseValidationResult.Valid(payload, payload.Plan);
            }
            catch (Exception ex)
            {
                Current.IsActivated = false;
                Current.LastError = ex.Message;
                SaveInternal(Current);

                return LicenseValidationResult.Invalid(
                    "خطا در اعتبارسنجی لایسنس: " + ex.Message);
            }
        }
    }

    public LicenseValidationResult Activate(
        string licenseJsonBase64,
        string signatureBase64)
    {
        lock (_sync)
        {
            Load();

            Current.LicenseJsonBase64 = Clean(licenseJsonBase64);
            Current.SignatureBase64 = Clean(signatureBase64);
            Current.LastCheckedAt = DateTime.UtcNow;
            Current.LastError = string.Empty;

            SaveInternal(Current);

            LicenseValidationResult result = ValidateCurrent();

            LicenseChanged?.Invoke(this, EventArgs.Empty);
            return result;
        }
    }

    public LicenseValidationResult ActivateFromCombinedText(
        string combinedText)
    {
        if (string.IsNullOrWhiteSpace(combinedText))
        {
            return LicenseValidationResult.Invalid("متن لایسنس خالی است.");
        }

        string text = combinedText.Trim();

        string payload = string.Empty;
        string signature = string.Empty;

        if (text.Contains("::", StringComparison.Ordinal))
        {
            string[] parts = text.Split(
                new[] { "::" },
                StringSplitOptions.None);

            if (parts.Length >= 2)
            {
                payload = parts[0].Trim();
                signature = parts[1].Trim();
            }
        }
        else
        {
            string[] lines = text
                .Replace("\r", string.Empty)
                .Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            foreach (string line in lines)
            {
                if (line.StartsWith(
                    "PAYLOAD=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    payload = line["PAYLOAD=".Length..].Trim();
                }
                else if (line.StartsWith(
                    "SIGNATURE=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    signature = line["SIGNATURE=".Length..].Trim();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(signature))
        {
            return LicenseValidationResult.Invalid(
                "فرمت لایسنس معتبر نیست. فرمت قابل قبول: PAYLOAD=... و SIGNATURE=... یا PAYLOAD::SIGNATURE");
        }

        return Activate(payload, signature);
    }

    public void Deactivate()
    {
        lock (_sync)
        {
            Load();

            Current.IsActivated = false;
            Current.LicenseJsonBase64 = string.Empty;
            Current.SignatureBase64 = string.Empty;
            Current.Payload = null;
            Current.LastError = string.Empty;
            Current.LastCheckedAt = DateTime.UtcNow;

            SaveInternal(Current);

            LicenseChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool HasPremiumAccess()
    {
        LicenseValidationResult result = ValidateCurrent();
        return result.HasPremiumAccess;
    }

    public LicensePlan GetEffectivePlan()
    {
        LicenseValidationResult result = ValidateCurrent();
        return result.EffectivePlan;
    }

    public string GetMachineId()
    {
        try
        {
            string raw =
                Environment.MachineName + "|" +
                Environment.UserName + "|" +
                Environment.ProcessorCount + "|" +
                Environment.OSVersion.VersionString;

            byte[] bytes =
                SHA256.HashData(Encoding.UTF8.GetBytes(raw));

            return Convert.ToHexString(bytes);
        }
        catch
        {
            byte[] bytes =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(Environment.MachineName));

            return Convert.ToHexString(bytes);
        }
    }

    public string GetStateFilePath()
    {
        return StateFilePath;
    }

    public string CreateUnsignedLicensePayloadJson(
        string userName,
        string userEmail,
        LicensePlan plan,
        DateTime? expiresAt,
        bool bindToThisMachine)
    {
        var payload = new LicensePayload
        {
            LicenseId = Guid.NewGuid().ToString("N"),
            UserName = userName?.Trim() ?? string.Empty,
            UserEmail = userEmail?.Trim() ?? string.Empty,
            Plan = plan,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt?.ToUniversalTime(),
            MachineId = bindToThisMachine ? GetMachineId() : string.Empty,
            ProductCode = "Hayt",
            Version = "1"
        };

        return JsonSerializer.Serialize(payload, _jsonOptions);
    }

    private bool VerifySignature(
        byte[] payloadBytes,
        byte[] signatureBytes)
    {
        using RSA rsa = RSA.Create();

        rsa.ImportFromPem(PublicKeyPem);

        return rsa.VerifyData(
            payloadBytes,
            signatureBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }

    private void SaveInternal(LicenseState state)
    {
        if (!Directory.Exists(AppDirectory))
        {
            Directory.CreateDirectory(AppDirectory);
        }

        string json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(StateFilePath, json);
    }

    private static string Clean(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}

