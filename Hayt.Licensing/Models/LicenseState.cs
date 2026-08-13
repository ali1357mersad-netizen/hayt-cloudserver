using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

public sealed class LicenseState
{
    public bool IsActivated { get; set; }

    public string LicenseJsonBase64 { get; set; } = string.Empty;

    public string SignatureBase64 { get; set; } = string.Empty;

    public LicensePayload? Payload { get; set; }

    public DateTime FirstRunAt { get; set; } = DateTime.UtcNow;

    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;

    public int TrialDays { get; set; } = 14;

    public string LastError { get; set; } = string.Empty;

    public static LicenseState CreateDefault()
    {
        return new LicenseState
        {
            IsActivated = false,
            LicenseJsonBase64 = string.Empty,
            SignatureBase64 = string.Empty,
            Payload = null,
            FirstRunAt = DateTime.UtcNow,
            LastCheckedAt = DateTime.UtcNow,
            TrialDays = 14,
            LastError = string.Empty
        };
    }

    public DateTime TrialExpiresAt =>
        FirstRunAt.AddDays(TrialDays);

    public bool IsTrialActive =>
        !IsActivated &&
        DateTime.UtcNow <= TrialExpiresAt;

    public int TrialDaysLeft
    {
        get
        {
            if (!IsTrialActive)
            {
                return 0;
            }

            double days = (TrialExpiresAt - DateTime.UtcNow).TotalDays;
            return Math.Max(0, (int)Math.Ceiling(days));
        }
    }

    public LicensePlan EffectivePlan
    {
        get
        {
            if (IsActivated && Payload is not null && !Payload.IsExpired)
            {
                return Payload.Plan;
            }

            if (IsTrialActive)
            {
                return LicensePlan.Trial;
            }

            return LicensePlan.Free;
        }
    }

    public bool HasPremiumAccess
    {
        get
        {
            if (IsActivated && Payload is not null && !Payload.IsExpired)
            {
                return Payload.IsPremiumLike;
            }

            return false;
        }
    }

    public string StatusText
    {
        get
        {
            if (IsActivated && Payload is not null && !Payload.IsExpired)
            {
                return $"فعال - {Payload.PlanText}";
            }

            if (IsActivated && Payload is not null && Payload.IsExpired)
            {
                return "لایسنس منقضی شده";
            }

            if (IsTrialActive)
            {
                return $"آزمایشی - {TrialDaysLeft} روز باقی‌مانده";
            }

            return "رایگان / فعال‌نشده";
        }
    }
}

