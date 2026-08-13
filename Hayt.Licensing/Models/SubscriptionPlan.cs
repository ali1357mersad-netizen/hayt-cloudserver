using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// اطلاعات نمایشی یک پلن اشتراک.
/// قیمت نهایی و تأیید پرداخت باید از Backend معتبر دریافت شود.
/// </summary>
public sealed record SubscriptionPlan(
    string Id,
    string DisplayName,
    int DurationDays,
    bool IsLifetime,
    bool IsEnterprise)
{
    public static SubscriptionPlan Free { get; } =
        new("free", "رایگان", 0, false, false);

    public static SubscriptionPlan Trial { get; } =
        new("trial", "آزمایشی", 7, false, false);

    public static SubscriptionPlan Monthly { get; } =
        new("premium-monthly", "Premium ماهانه", 30, false, false);

    public static SubscriptionPlan Yearly { get; } =
        new("premium-yearly", "Premium سالانه", 365, false, false);

    public static SubscriptionPlan Lifetime { get; } =
        new("lifetime", "دائمی", 0, true, false);

    public static SubscriptionPlan Enterprise { get; } =
        new("enterprise", "سازمانی", 0, false, true);
}

