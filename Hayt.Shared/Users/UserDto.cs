using System;
using Hayt.Shared.Licensing;

namespace Hayt.Shared.Users;

public sealed class UserDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public LicensePlan Plan { get; set; } = LicensePlan.Free;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubscriptionExpiresAtUtc { get; set; }

    public bool HasPremiumAccess =>
        IsActive &&
        Plan != LicensePlan.Free &&
        (SubscriptionExpiresAtUtc == null || SubscriptionExpiresAtUtc > DateTimeOffset.UtcNow);
}
