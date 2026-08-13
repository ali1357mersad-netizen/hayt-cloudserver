using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

public sealed class LicensePayload
{
    public string LicenseId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public LicensePlan Plan { get; set; } = LicensePlan.Free;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public string MachineId { get; set; } = string.Empty;

    public string ProductCode { get; set; } = "Hayt";

    public string Version { get; set; } = "1";

    public bool IsExpired =>
        ExpiresAt.HasValue &&
        DateTime.UtcNow > ExpiresAt.Value.ToUniversalTime();

    public bool IsPremiumLike =>
        Plan == LicensePlan.Premium ||
        Plan == LicensePlan.Lifetime ||
        Plan == LicensePlan.Enterprise;

    public string DisplayUser =>
        string.IsNullOrWhiteSpace(UserName)
            ? UserEmail
            : UserName;

    public string ExpiryText
    {
        get
        {
            if (!ExpiresAt.HasValue)
            {
                return "بدون تاریخ انقضا";
            }

            return ExpiresAt.Value.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
        }
    }

    public string PlanText
    {
        get
        {
            return Plan switch
            {
                LicensePlan.Free => "رایگان",
                LicensePlan.Trial => "آزمایشی",
                LicensePlan.Premium => "Premium",
                LicensePlan.Lifetime => "دائمی",
                LicensePlan.Enterprise => "سازمانی",
                _ => "نامشخص"
            };
        }
    }
}

