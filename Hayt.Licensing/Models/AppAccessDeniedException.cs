using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

/// <summary>
/// وقتی عملیات برنامه از نظر Role یا License مجاز نباشد پرتاب می‌شود.
/// </summary>
public sealed class AppAccessDeniedException : UnauthorizedAccessException
{
    public AppFeature Feature { get; }

    public UserRole Role { get; }

    public LicensePlan EffectivePlan { get; }

    public string FeatureTitle { get; }

    public string ReasonCode { get; }

    public bool RequiresUpgrade { get; }

    public bool RequiresHigherRole { get; }

    public AppAccessDeniedException(AppAccessDecision decision)
        : base(decision?.Message ?? "دسترسی به این عملیات مجاز نیست.")
    {
        if (decision is null)
        {
            throw new ArgumentNullException(nameof(decision));
        }

        Feature = decision.Feature;
        Role = decision.Role;
        EffectivePlan = decision.EffectivePlan;
        FeatureTitle = decision.FeatureTitle;
        ReasonCode = decision.ReasonCode;
        RequiresUpgrade = decision.RequiresUpgrade;
        RequiresHigherRole = decision.RequiresHigherRole;
    }
}

