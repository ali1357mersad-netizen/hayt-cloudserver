using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// وضعیت تجاری اشتراک.
/// تصمیم امنیتی نهایی باید توسط License Gate گرفته شود.
/// </summary>
public enum SubscriptionStatus
{
    Free = 0,
    Trial = 1,
    Active = 2,
    GracePeriod = 3,
    Expired = 4,
    Lifetime = 5,
    Enterprise = 6,
    Invalid = 7
}

