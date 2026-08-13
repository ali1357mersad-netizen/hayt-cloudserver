using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

/// <summary>
/// تصویر لحظه‌ای وضعیت اشتراک برای UI.
///
/// هشدار امنیتی:
/// این مدل به‌تنهایی مجوز دسترسی صادر نمی‌کند. دسترسی واقعی باید
/// توسط LicenseService و PremiumAccessService تأیید شود.
/// </summary>
public sealed record SubscriptionSnapshot
{
    public SubscriptionStatus Status { get; init; } =
        SubscriptionStatus.Free;

    public string PlanId { get; init; } = "free";

    public string PlanName { get; init; } = "رایگان";

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? ExpiresAtUtc { get; init; }

    public DateTimeOffset? GraceEndsAtUtc { get; init; }

    public DateTimeOffset CheckedAtUtc { get; init; } =
        DateTimeOffset.UtcNow;

    public string? LicenseId { get; init; }

    public string? MachineId { get; init; }

    public string? Message { get; init; }

    public bool IsCryptographicallyValidated { get; init; }

    public bool HasPremiumAccess =>
        IsCryptographicallyValidated &&
        Status is
            SubscriptionStatus.Trial or
            SubscriptionStatus.Active or
            SubscriptionStatus.GracePeriod or
            SubscriptionStatus.Lifetime or
            SubscriptionStatus.Enterprise;

    public bool IsLifetime =>
        Status == SubscriptionStatus.Lifetime;

    public bool IsExpired =>
        Status is
            SubscriptionStatus.Expired or
            SubscriptionStatus.Invalid;

    public int? RemainingDays
    {
        get
        {
            if (IsLifetime || ExpiresAtUtc is null)
            {
                return null;
            }

            var remaining = ExpiresAtUtc.Value - DateTimeOffset.UtcNow;

            if (remaining <= TimeSpan.Zero)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Ceiling(remaining.TotalDays));
        }
    }

    public static SubscriptionSnapshot Free(
        string? message = null)
    {
        return new SubscriptionSnapshot
        {
            Status = SubscriptionStatus.Free,
            PlanId = "free",
            PlanName = "رایگان",
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Message = message ??
                "در حال حاضر اشتراک Premium فعالی وجود ندارد.",
            IsCryptographicallyValidated = false
        };
    }

    public static SubscriptionSnapshot Invalid(
        string message)
    {
        return new SubscriptionSnapshot
        {
            Status = SubscriptionStatus.Invalid,
            PlanId = "invalid",
            PlanName = "نامعتبر",
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Message = message,
            IsCryptographicallyValidated = false
        };
    }
}

