using Hayt.Licensing.Services;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس مرکزی وضعیت اشتراک.
///
/// اصل امنیتی:
/// فایل JSON یا Cache فقط برای استخراج اطلاعات نمایشی استفاده می‌شود.
/// اعطای دسترسی منحصراً با نتیجه PremiumAccessService انجام می‌شود.
/// </summary>
public sealed class SubscriptionService : ISubscriptionService
{
    private static readonly TimeSpan CacheLifetime =
        TimeSpan.FromMinutes(5);

    private static readonly TimeSpan DefaultGracePeriod =
        TimeSpan.FromDays(3);

    private readonly IPremiumAccessService _premiumAccessService;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private SubscriptionSnapshot _current =
        SubscriptionSnapshot.Free();

    private DateTimeOffset _lastRefreshUtc =
        DateTimeOffset.MinValue;

    public SubscriptionService()
        : this(new PremiumAccessService())
    {
    }

    public SubscriptionService(
        IPremiumAccessService premiumAccessService)
    {
        _premiumAccessService = premiumAccessService ??
            throw new ArgumentNullException(
                nameof(premiumAccessService));
    }

    public event EventHandler<SubscriptionSnapshot>?
        SubscriptionChanged;

    public SubscriptionSnapshot Current => _current;

    public bool HasPremiumAccess()
    {
        try
        {
            // منبع نهایی تصمیم، Gate رمزنگاری‌شده مرحله‌های قبلی است.
            _premiumAccessService.EnsureAccess(
                PremiumFeature.RealAITutor,
                forceRefresh: false);

            return true;
        }
        catch (PremiumAccessDeniedException)
        {
            return false;
        }
        catch
        {
            // Fail closed: خطای ناشناخته نباید دسترسی ایجاد کند.
            return false;
        }
    }

    public void InvalidateCache()
    {
        _lastRefreshUtc = DateTimeOffset.MinValue;
    }

    public async Task<SubscriptionSnapshot> RefreshAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (
            !forceRefresh &&
            DateTimeOffset.UtcNow - _lastRefreshUtc <
                CacheLifetime
        )
        {
            return _current;
        }

        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            if (
                !forceRefresh &&
                DateTimeOffset.UtcNow - _lastRefreshUtc <
                    CacheLifetime
            )
            {
                return _current;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var hasValidatedAccess =
                ValidatePremiumAccess(forceRefresh);

            var metadata = TryReadDisplayMetadata();

            var next = BuildSnapshot(
                hasValidatedAccess,
                metadata,
                DateTimeOffset.UtcNow);

            var changed = !Equals(_current, next);

            _current = next;
            _lastRefreshUtc = DateTimeOffset.UtcNow;

            if (changed)
            {
                SubscriptionChanged?.Invoke(this, _current);
            }

            return _current;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool ValidatePremiumAccess(bool forceRefresh)
    {
        try
        {
            _premiumAccessService.EnsureAccess(
                PremiumFeature.RealAITutor,
                forceRefresh);

            return true;
        }
        catch (PremiumAccessDeniedException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static SubscriptionSnapshot BuildSnapshot(
        bool hasValidatedAccess,
        DisplayMetadata? metadata,
        DateTimeOffset now)
    {
        if (!hasValidatedAccess)
        {
            if (
                metadata?.ExpiresAtUtc is not null &&
                metadata.ExpiresAtUtc <= now
            )
            {
                return new SubscriptionSnapshot
                {
                    Status = SubscriptionStatus.Expired,
                    PlanId = metadata.PlanId ?? "expired",
                    PlanName = metadata.PlanName ?? "منقضی‌شده",
                    StartedAtUtc = metadata.StartedAtUtc,
                    ExpiresAtUtc = metadata.ExpiresAtUtc,
                    GraceEndsAtUtc = metadata.GraceEndsAtUtc,
                    CheckedAtUtc = now,
                    LicenseId = metadata.LicenseId,
                    MachineId = metadata.MachineId,
                    Message =
                        "اشتراک یا لایسنس منقضی شده است.",
                    IsCryptographicallyValidated = false
                };
            }

            return SubscriptionSnapshot.Free(
                "اشتراک Premium معتبر شناسایی نشد.");
        }

        // Lifetime و Enterprise تاریخ انقضا ندارند یا نوعشان صریح است.
        if (
            IsValue(metadata?.LicenseType, "lifetime") ||
            IsValue(metadata?.PlanId, "lifetime")
        )
        {
            return CreateValidatedSnapshot(
                SubscriptionStatus.Lifetime,
                "lifetime",
                "دائمی",
                metadata,
                now,
                "لایسنس دائمی معتبر است.");
        }

        if (
            IsValue(metadata?.LicenseType, "enterprise") ||
            IsValue(metadata?.PlanId, "enterprise")
        )
        {
            return CreateValidatedSnapshot(
                SubscriptionStatus.Enterprise,
                "enterprise",
                "سازمانی",
                metadata,
                now,
                "لایسنس سازمانی معتبر است.");
        }

        if (
            IsValue(metadata?.LicenseType, "trial") ||
            IsValue(metadata?.PlanId, "trial")
        )
        {
            return CreateValidatedSnapshot(
                SubscriptionStatus.Trial,
                metadata?.PlanId ?? "trial",
                metadata?.PlanName ?? "آزمایشی",
                metadata,
                now,
                "دوره آزمایشی معتبر فعال است.");
        }

        if (metadata?.ExpiresAtUtc is not null)
        {
            var expires = metadata.ExpiresAtUtc.Value;
            var graceEnd =
                metadata.GraceEndsAtUtc ??
                expires.Add(DefaultGracePeriod);

            if (expires > now)
            {
                return CreateValidatedSnapshot(
                    SubscriptionStatus.Active,
                    metadata.PlanId ?? "premium",
                    metadata.PlanName ?? "Premium",
                    metadata,
                    now,
                    "اشتراک Premium فعال است.");
            }

            // تنها اگر License Gate دسترسی را تأیید کرده باشد،
            // وضعیت Grace Period می‌تواند دارای دسترسی باشد.
            if (graceEnd > now)
            {
                var withGrace = metadata with
                {
                    GraceEndsAtUtc = graceEnd
                };

                return CreateValidatedSnapshot(
                    SubscriptionStatus.GracePeriod,
                    metadata.PlanId ?? "premium",
                    metadata.PlanName ?? "Premium",
                    withGrace,
                    now,
                    "اشتراک در مهلت ارفاقی قرار دارد.");
            }
        }

        return CreateValidatedSnapshot(
            SubscriptionStatus.Active,
            metadata?.PlanId ?? "premium",
            metadata?.PlanName ?? "Premium",
            metadata,
            now,
            "دسترسی Premium توسط License Gate تأیید شد.");
    }

    private static SubscriptionSnapshot CreateValidatedSnapshot(
        SubscriptionStatus status,
        string planId,
        string planName,
        DisplayMetadata? metadata,
        DateTimeOffset now,
        string message)
    {
        return new SubscriptionSnapshot
        {
            Status = status,
            PlanId = planId,
            PlanName = planName,
            StartedAtUtc = metadata?.StartedAtUtc,
            ExpiresAtUtc = metadata?.ExpiresAtUtc,
            GraceEndsAtUtc = metadata?.GraceEndsAtUtc,
            CheckedAtUtc = now,
            LicenseId = metadata?.LicenseId,
            MachineId = metadata?.MachineId,
            Message = message,
            IsCryptographicallyValidated = true
        };
    }

    private static bool IsValue(
        string? value,
        string expected)
    {
        return string.Equals(
            value?.Trim(),
            expected,
            StringComparison.OrdinalIgnoreCase);
    }

    private static DisplayMetadata? TryReadDisplayMetadata()
    {
        foreach (var path in GetCandidateLicensePaths())
        {
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                using var stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);

                var root = document.RootElement;

                // برخی خروجی‌ها Payload را داخل property نگه می‌دارند.
                if (
                    TryGetPropertyIgnoreCase(
                        root,
                        "payload",
                        out var payload) &&
                    payload.ValueKind == JsonValueKind.Object
                )
                {
                    root = payload;
                }

                return new DisplayMetadata
                {
                    PlanId = ReadString(
                        root,
                        "planId",
                        "plan",
                        "subscriptionPlan"),

                    PlanName = ReadString(
                        root,
                        "planName",
                        "displayName"),

                    LicenseType = ReadString(
                        root,
                        "licenseType",
                        "type",
                        "edition"),

                    LicenseId = ReadString(
                        root,
                        "licenseId",
                        "id",
                        "serial"),

                    MachineId = ReadString(
                        root,
                        "machineId",
                        "deviceId"),

                    StartedAtUtc = ReadDate(
                        root,
                        "startedAtUtc",
                        "startDate",
                        "issuedAt",
                        "validFrom"),

                    ExpiresAtUtc = ReadDate(
                        root,
                        "expiresAtUtc",
                        "expiryDate",
                        "expiresAt",
                        "validTo"),

                    GraceEndsAtUtc = ReadDate(
                        root,
                        "graceEndsAtUtc",
                        "graceEnd",
                        "graceUntil")
                };
            }
            catch
            {
                // Metadata خراب نباید باعث Crash یا اعطای دسترسی شود.
            }
        }

        return null;
    }

    private static string[] GetCandidateLicensePaths()
    {
        var baseDirectory = AppContext.BaseDirectory;

        var appData = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Hayt");

        return new[]
        {
            Path.Combine(baseDirectory, "license.json"),
            Path.Combine(baseDirectory, "License", "license.json"),
            Path.Combine(appData, "license.json"),
            Path.Combine(appData, "License", "license.json")
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    }

    private static string? ReadString(
        JsonElement root,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (
                TryGetPropertyIgnoreCase(root, name, out var value) &&
                value.ValueKind == JsonValueKind.String
            )
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static DateTimeOffset? ReadDate(
        JsonElement root,
        params string[] names)
    {
        var text = ReadString(root, names);

        if (
            DateTimeOffset.TryParse(
                text,
                null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var result)
        )
        {
            return result;
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (
                    string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private sealed record DisplayMetadata
    {
        public string? PlanId { get; init; }

        public string? PlanName { get; init; }

        public string? LicenseType { get; init; }

        public string? LicenseId { get; init; }

        public string? MachineId { get; init; }

        public DateTimeOffset? StartedAtUtc { get; init; }

        public DateTimeOffset? ExpiresAtUtc { get; init; }

        public DateTimeOffset? GraceEndsAtUtc { get; init; }
    }
}

