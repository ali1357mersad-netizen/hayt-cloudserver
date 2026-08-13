using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// مدل آماده برای نمایش وضعیت دسترسی در UI.
/// مثلاً در داشبورد امنیتی، صفحه تنظیمات یا پنل استاد.
/// </summary>
public sealed class AppAccessStatusItem
{
    public AppFeature Feature { get; init; }

    public string FeatureTitle { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public LicensePlan EffectivePlan { get; init; }

    public bool IsAllowed { get; init; }

    public bool RoleAllowed { get; init; }

    public bool LicenseAllowed { get; init; }

    public bool RequiresUpgrade { get; init; }

    public bool RequiresHigherRole { get; init; }

    public string Message { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    public string StatusText
    {
        get
        {
            if (IsAllowed)
            {
                return "مجاز";
            }

            if (RequiresHigherRole)
            {
                return "نیازمند نقش بالاتر";
            }

            if (RequiresUpgrade)
            {
                return "نیازمند ارتقای لایسنس";
            }

            return "غیرمجاز";
        }
    }
}

