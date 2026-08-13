using Hayt.Licensing.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// License Gate مرکزی.
///
/// نکته امنیتی:
/// پنهان‌کردن دکمه در UI محافظت محسوب نمی‌شود.
/// هر عملیات حساس باید پیش از اجرا EnsureAccess را فراخوانی کند
/// یا از Execute/ExecuteAsync عبور کند.
/// </summary>
public sealed class PremiumAccessService : IPremiumAccessService
{
    private static readonly TimeSpan SuccessfulDecisionCacheDuration =
        TimeSpan.FromSeconds(20);

    private readonly ILicenseService _licenseService;
    private readonly object _sync = new();

    private readonly Dictionary<PremiumFeature, CachedDecision> _cache = new();

    public event EventHandler? AccessStateChanged;

    public PremiumAccessService()
        : this(new LicenseService())
    {
    }

    public PremiumAccessService(ILicenseService licenseService)
    {
        _licenseService = licenseService ??
            throw new ArgumentNullException(nameof(licenseService));

        _licenseService.LicenseChanged += OnLicenseChanged;
    }

    public FeatureAccessDecision CheckAccess(
        PremiumFeature feature,
        bool forceRefresh = false)
    {
        if (!Enum.IsDefined(typeof(PremiumFeature), feature))
        {
            return FeatureAccessDecision.Deny(
                feature,
                LicensePlan.Free,
                "قابلیت ناشناخته",
                "قابلیت درخواستی در سیاست امنیتی ثبت نشده است.",
                "UNKNOWN_FEATURE");
        }

        PremiumFeatureTier tier = PremiumFeaturePolicy.GetTier(feature);
        string title = PremiumFeaturePolicy.GetTitle(feature);

        if (tier == PremiumFeatureTier.Free)
        {
            return FeatureAccessDecision.Allow(
                feature,
                LicensePlan.Free,
                title,
                "این قابلیت در نسخه رایگان قابل استفاده است.",
                isLicensed: false,
                isTrialAccess: false);
        }

        if (!forceRefresh &&
            TryGetCachedDecision(feature, out FeatureAccessDecision cached))
        {
            return cached;
        }

        FeatureAccessDecision decision;

        try
        {
            LicenseValidationResult validation =
                _licenseService.ValidateCurrent();

            decision = Evaluate(feature, tier, title, validation);
        }
        catch
        {
            decision = FeatureAccessDecision.Deny(
                feature,
                LicensePlan.Free,
                title,
                $"اعتبارسنجی امنیتی «{title}» انجام نشد. دسترسی برای حفظ امنیت مسدود شد.",
                "LICENSE_VALIDATION_ERROR");
        }

        CacheOnlySuccessfulDecision(feature, decision);
        return decision;
    }

    public bool CanAccess(
        PremiumFeature feature,
        bool forceRefresh = false)
    {
        return CheckAccess(feature, forceRefresh).IsAllowed;
    }

    public void EnsureAccess(
        PremiumFeature feature,
        bool forceRefresh = true)
    {
        FeatureAccessDecision decision =
            CheckAccess(feature, forceRefresh);

        if (!decision.IsAllowed)
        {
            throw new PremiumAccessDeniedException(decision);
        }
    }

    public void EnsurePremiumAccess(bool forceRefresh = true)
    {
        EnsureAccess(PremiumFeature.RealAITutor, forceRefresh);
    }

    public T Execute<T>(
        PremiumFeature feature,
        Func<T> operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        EnsureAccess(feature, forceRefresh: true);
        return operation();
    }

    public void Execute(
        PremiumFeature feature,
        Action operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        EnsureAccess(feature, forceRefresh: true);
        operation();
    }

    public async Task<T> ExecuteAsync<T>(
        PremiumFeature feature,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        cancellationToken.ThrowIfCancellationRequested();

        EnsureAccess(feature, forceRefresh: true);

        cancellationToken.ThrowIfCancellationRequested();

        return await operation(cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(
        PremiumFeature feature,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        cancellationToken.ThrowIfCancellationRequested();

        EnsureAccess(feature, forceRefresh: true);

        cancellationToken.ThrowIfCancellationRequested();

        await operation(cancellationToken).ConfigureAwait(false);
    }

    public void Invalidate()
    {
        lock (_sync)
        {
            _cache.Clear();
        }

        AccessStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static FeatureAccessDecision Evaluate(
        PremiumFeature feature,
        PremiumFeatureTier tier,
        string title,
        LicenseValidationResult validation)
    {
        LicensePlan plan = validation.EffectivePlan;

        bool paidLicenseValid =
            validation.IsValid &&
            validation.IsActivated &&
            !validation.IsExpired &&
            validation.Payload is not null &&
            (
                plan == LicensePlan.Premium ||
                plan == LicensePlan.Lifetime ||
                plan == LicensePlan.Enterprise
            );

        bool enterpriseLicenseValid =
            paidLicenseValid &&
            plan == LicensePlan.Enterprise;

        bool trialValid =
            validation.IsValid &&
            validation.IsTrial &&
            plan == LicensePlan.Trial;

        switch (tier)
        {
            case PremiumFeatureTier.Premium:
            {
                if (paidLicenseValid)
                {
                    return FeatureAccessDecision.Allow(
                        feature,
                        plan,
                        title,
                        $"دسترسی به «{title}» با لایسنس {plan} مجاز است.",
                        isLicensed: true,
                        isTrialAccess: false);
                }

                if (trialValid)
                {
                    return FeatureAccessDecision.Allow(
                        feature,
                        LicensePlan.Trial,
                        title,
                        $"دسترسی آزمایشی به «{title}» مجاز است.",
                        isLicensed: false,
                        isTrialAccess: true);
                }

                return FeatureAccessDecision.Deny(
                    feature,
                    plan,
                    title,
                    $"قابلیت «{title}» به Trial فعال یا لایسنس Premium معتبر نیاز دارد.",
                    validation.IsExpired
                        ? "LICENSE_EXPIRED"
                        : "PREMIUM_REQUIRED");
            }

            case PremiumFeatureTier.PaidLicenseOnly:
            {
                if (paidLicenseValid)
                {
                    return FeatureAccessDecision.Allow(
                        feature,
                        plan,
                        title,
                        $"دسترسی به «{title}» با لایسنس معتبر مجاز است.",
                        isLicensed: true,
                        isTrialAccess: false);
                }

                return FeatureAccessDecision.Deny(
                    feature,
                    plan,
                    title,
                    $"قابلیت «{title}» در Trial فعال نیست و به لایسنس واقعی نیاز دارد.",
                    validation.IsExpired
                        ? "LICENSE_EXPIRED"
                        : "PAID_LICENSE_REQUIRED");
            }

            case PremiumFeatureTier.Enterprise:
            {
                if (enterpriseLicenseValid)
                {
                    return FeatureAccessDecision.Allow(
                        feature,
                        LicensePlan.Enterprise,
                        title,
                        $"دسترسی سازمانی به «{title}» مجاز است.",
                        isLicensed: true,
                        isTrialAccess: false);
                }

                return FeatureAccessDecision.Deny(
                    feature,
                    plan,
                    title,
                    $"قابلیت «{title}» فقط با لایسنس Enterprise در دسترس است.",
                    "ENTERPRISE_REQUIRED");
            }

            default:
            {
                return FeatureAccessDecision.Deny(
                    feature,
                    plan,
                    title,
                    "سیاست دسترسی این قابلیت معتبر نیست.",
                    "INVALID_FEATURE_POLICY");
            }
        }
    }

    private bool TryGetCachedDecision(
        PremiumFeature feature,
        out FeatureAccessDecision decision)
    {
        lock (_sync)
        {
            if (_cache.TryGetValue(feature, out CachedDecision? cached) &&
                DateTime.UtcNow <= cached.ValidUntilUtc)
            {
                decision = cached.Decision;
                return true;
            }

            _cache.Remove(feature);
        }

        decision = null!;
        return false;
    }

    private void CacheOnlySuccessfulDecision(
        PremiumFeature feature,
        FeatureAccessDecision decision)
    {
        if (!decision.IsAllowed)
        {
            lock (_sync)
            {
                _cache.Remove(feature);
            }

            return;
        }

        lock (_sync)
        {
            _cache[feature] = new CachedDecision(
                decision,
                DateTime.UtcNow.Add(SuccessfulDecisionCacheDuration));
        }
    }

    private void OnLicenseChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    private sealed record CachedDecision(
        FeatureAccessDecision Decision,
        DateTime ValidUntilUtc);
}

