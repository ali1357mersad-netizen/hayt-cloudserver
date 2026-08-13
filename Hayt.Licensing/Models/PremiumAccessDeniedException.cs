using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

/// <summary>
/// زمانی پرتاب می‌شود که یک عملیات محافظت‌شده بدون مجوز معتبر فراخوانی شود.
/// این Exception باید در مرز UI مدیریت شود، نه داخل سرویس حساس.
/// </summary>
public sealed class PremiumAccessDeniedException : UnauthorizedAccessException
{
    public PremiumFeature Feature { get; }

    public LicensePlan EffectivePlan { get; }

    public string FeatureTitle { get; }

    public string ReasonCode { get; }

    public bool RequiresUpgrade { get; }

    public PremiumAccessDeniedException(FeatureAccessDecision decision)
        : base(decision?.Message ?? "دسترسی به قابلیت Premium مجاز نیست.")
    {
        if (decision is null)
        {
            throw new ArgumentNullException(nameof(decision));
        }

        Feature = decision.Feature;
        EffectivePlan = decision.EffectivePlan;
        FeatureTitle = decision.FeatureTitle;
        ReasonCode = decision.ReasonCode;
        RequiresUpgrade = decision.RequiresUpgrade;
    }
}

