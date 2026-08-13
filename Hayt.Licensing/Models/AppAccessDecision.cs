using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// نتیجه نهایی دسترسی برنامه.
/// این تصمیم ترکیبی از Role و License است.
/// </summary>
public sealed class AppAccessDecision
{
    public AppFeature Feature { get; init; }

    public UserRole Role { get; init; } = UserRole.Guest;

    public LicensePlan EffectivePlan { get; init; } = LicensePlan.Free;

    public bool IsAllowed { get; init; }

    public bool RoleAllowed { get; init; }

    public bool LicenseAllowed { get; init; }

    public bool RequiresUpgrade { get; init; }

    public bool RequiresHigherRole { get; init; }

    public string FeatureTitle { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    public static AppAccessDecision Allow(
        AppFeature feature,
        UserRole role,
        LicensePlan effectivePlan,
        string title,
        string message,
        bool licenseAllowed)
    {
        return new AppAccessDecision
        {
            Feature = feature,
            Role = role,
            EffectivePlan = effectivePlan,
            IsAllowed = true,
            RoleAllowed = true,
            LicenseAllowed = licenseAllowed,
            RequiresUpgrade = false,
            RequiresHigherRole = false,
            FeatureTitle = title,
            Message = message,
            ReasonCode = "ALLOWED"
        };
    }

    public static AppAccessDecision DenyByRole(
        AppFeature feature,
        UserRole role,
        LicensePlan effectivePlan,
        string title,
        string message)
    {
        return new AppAccessDecision
        {
            Feature = feature,
            Role = role,
            EffectivePlan = effectivePlan,
            IsAllowed = false,
            RoleAllowed = false,
            LicenseAllowed = true,
            RequiresUpgrade = false,
            RequiresHigherRole = true,
            FeatureTitle = title,
            Message = message,
            ReasonCode = "ROLE_DENIED"
        };
    }

    public static AppAccessDecision DenyByLicense(
        AppFeature feature,
        UserRole role,
        LicensePlan effectivePlan,
        string title,
        string message,
        string reasonCode)
    {
        return new AppAccessDecision
        {
            Feature = feature,
            Role = role,
            EffectivePlan = effectivePlan,
            IsAllowed = false,
            RoleAllowed = true,
            LicenseAllowed = false,
            RequiresUpgrade = true,
            RequiresHigherRole = false,
            FeatureTitle = title,
            Message = message,
            ReasonCode = reasonCode
        };
    }

    public static AppAccessDecision DenyUnknown(
        AppFeature feature,
        UserRole role,
        LicensePlan effectivePlan)
    {
        return new AppAccessDecision
        {
            Feature = feature,
            Role = role,
            EffectivePlan = effectivePlan,
            IsAllowed = false,
            RoleAllowed = false,
            LicenseAllowed = false,
            RequiresUpgrade = false,
            RequiresHigherRole = false,
            FeatureTitle = feature.ToString(),
            Message = "قابلیت درخواستی در سیاست دسترسی برنامه ثبت نشده است.",
            ReasonCode = "UNKNOWN_APP_FEATURE"
        };
    }
}

