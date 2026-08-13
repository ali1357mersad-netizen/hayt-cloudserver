using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

public sealed class LicenseValidationResult
{
    public bool IsValid { get; init; }

    public bool IsActivated { get; init; }

    public bool IsTrial { get; init; }

    public bool IsExpired { get; init; }

    public bool MachineMismatch { get; init; }

    public string Message { get; init; } = string.Empty;

    public LicensePayload? Payload { get; init; }

    public LicensePlan EffectivePlan { get; init; } = LicensePlan.Free;

    public bool HasPremiumAccess =>
        IsValid &&
        Payload is not null &&
        Payload.IsPremiumLike &&
        !IsExpired;

    public static LicenseValidationResult Valid(
        LicensePayload payload,
        LicensePlan effectivePlan)
    {
        return new LicenseValidationResult
        {
            IsValid = true,
            IsActivated = true,
            IsTrial = false,
            IsExpired = false,
            MachineMismatch = false,
            Message = "لایسنس معتبر است.",
            Payload = payload,
            EffectivePlan = effectivePlan
        };
    }

    public static LicenseValidationResult Trial(int daysLeft)
    {
        return new LicenseValidationResult
        {
            IsValid = true,
            IsActivated = false,
            IsTrial = true,
            IsExpired = false,
            MachineMismatch = false,
            Message = $"حالت آزمایشی فعال است. {daysLeft} روز باقی‌مانده.",
            Payload = null,
            EffectivePlan = LicensePlan.Trial
        };
    }

    public static LicenseValidationResult Invalid(
        string message,
        bool isExpired = false,
        bool machineMismatch = false)
    {
        return new LicenseValidationResult
        {
            IsValid = false,
            IsActivated = false,
            IsTrial = false,
            IsExpired = isExpired,
            MachineMismatch = machineMismatch,
            Message = message,
            Payload = null,
            EffectivePlan = LicensePlan.Free
        };
    }
}

