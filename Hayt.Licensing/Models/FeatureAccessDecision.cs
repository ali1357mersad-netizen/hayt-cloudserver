using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// نتیجه کامل ارزیابی دسترسی به یک قابلیت.
/// UI می‌تواند برای نمایش وضعیت از این مدل استفاده کند؛
/// اما اجرای واقعی عملیات باید از EnsureAccess عبور کند.
/// </summary>
public sealed class FeatureAccessDecision
{
    public PremiumFeature Feature { get; init; }

    public bool IsAllowed { get; init; }

    public bool IsLicensed { get; init; }

    public bool IsTrialAccess { get; init; }

    public bool RequiresUpgrade { get; init; }

    public LicensePlan EffectivePlan { get; init; } = LicensePlan.Free;

    public string FeatureTitle { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    public static FeatureAccessDecision Allow(
        PremiumFeature feature,
        LicensePlan effectivePlan,
        string title,
        string message,
        bool isLicensed,
        bool isTrialAccess)
    {
        return new FeatureAccessDecision
        {
            Feature = feature,
            IsAllowed = true,
            IsLicensed = isLicensed,
            IsTrialAccess = isTrialAccess,
            RequiresUpgrade = false,
            EffectivePlan = effectivePlan,
            FeatureTitle = title,
            Message = message,
            ReasonCode = isTrialAccess
                ? "TRIAL_ACCESS"
                : isLicensed
                    ? "LICENSED_ACCESS"
                    : "FREE_ACCESS"
        };
    }

    public static FeatureAccessDecision Deny(
        PremiumFeature feature,
        LicensePlan effectivePlan,
        string title,
        string message,
        string reasonCode)
    {
        return new FeatureAccessDecision
        {
            Feature = feature,
            IsAllowed = false,
            IsLicensed = false,
            IsTrialAccess = false,
            RequiresUpgrade = true,
            EffectivePlan = effectivePlan,
            FeatureTitle = title,
            Message = message,
            ReasonCode = reasonCode
        };
    }
}

