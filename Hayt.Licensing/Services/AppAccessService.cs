using Hayt.Licensing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// Gate نهایی برنامه.
/// اصل امنیتی:
/// دسترسی نهایی = اجازه Role + اجازه License.
/// UI فقط می‌تواند وضعیت را نمایش دهد؛ عملیات حساس باید از این سرویس عبور کند.
/// </summary>
public sealed class AppAccessService : IAppAccessService
{
    private readonly IRoleAccessService _roleAccessService;
    private readonly IPremiumAccessService _premiumAccessService;
    private readonly ILicenseService _licenseService;

    public AppAccessService()
        : this(
            new RoleAccessService(),
            new PremiumAccessService(),
            new LicenseService())
    {
    }

    public AppAccessService(
        IRoleAccessService roleAccessService,
        IPremiumAccessService premiumAccessService,
        ILicenseService licenseService)
    {
        _roleAccessService = roleAccessService ??
            throw new ArgumentNullException(nameof(roleAccessService));

        _premiumAccessService = premiumAccessService ??
            throw new ArgumentNullException(nameof(premiumAccessService));

        _licenseService = licenseService ??
            throw new ArgumentNullException(nameof(licenseService));
    }

    public AppAccessDecision CheckAccess(
        AppFeature feature,
        bool forceLicenseRefresh = false)
    {
        UserRole role = _roleAccessService.CurrentRole;
        LicensePlan plan = SafeGetEffectivePlan();
        string title = AppAccessPolicy.GetTitle(feature);

        if (!AppAccessPolicy.IsKnownFeature(feature))
        {
            return AppAccessDecision.DenyUnknown(feature, role, plan);
        }

        bool roleAllowed = AppAccessPolicy.IsRoleAllowed(role, feature);

        if (!roleAllowed)
        {
            return AppAccessDecision.DenyByRole(
                feature,
                role,
                plan,
                title,
                $"نقش «{GetRoleText(role)}» اجازه دسترسی به «{title}» را ندارد.");
        }

        PremiumFeature? requiredPremiumFeature =
            AppAccessPolicy.GetRequiredPremiumFeature(feature);

        if (!requiredPremiumFeature.HasValue)
        {
            return AppAccessDecision.Allow(
                feature,
                role,
                plan,
                title,
                $"دسترسی به «{title}» برای نقش «{GetRoleText(role)}» مجاز است.",
                licenseAllowed: true);
        }

        FeatureAccessDecision premiumDecision =
            _premiumAccessService.CheckAccess(
                requiredPremiumFeature.Value,
                forceLicenseRefresh);

        plan = premiumDecision.EffectivePlan;

        if (!premiumDecision.IsAllowed)
        {
            return AppAccessDecision.DenyByLicense(
                feature,
                role,
                plan,
                title,
                premiumDecision.Message,
                premiumDecision.ReasonCode);
        }

        return AppAccessDecision.Allow(
            feature,
            role,
            plan,
            title,
            $"دسترسی به «{title}» برای نقش «{GetRoleText(role)}» و پلن «{plan}» مجاز است.",
            licenseAllowed: true);
    }

    public bool CanAccess(
        AppFeature feature,
        bool forceLicenseRefresh = false)
    {
        return CheckAccess(feature, forceLicenseRefresh).IsAllowed;
    }

    public void EnsureAccess(
        AppFeature feature,
        bool forceLicenseRefresh = true)
    {
        AppAccessDecision decision =
            CheckAccess(feature, forceLicenseRefresh);

        if (!decision.IsAllowed)
        {
            throw new AppAccessDeniedException(decision);
        }
    }

    public T Execute<T>(
        AppFeature feature,
        Func<T> operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        EnsureAccess(feature, forceLicenseRefresh: true);
        return operation();
    }

    public void Execute(
        AppFeature feature,
        Action operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        EnsureAccess(feature, forceLicenseRefresh: true);
        operation();
    }

    public async Task<T> ExecuteAsync<T>(
        AppFeature feature,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        cancellationToken.ThrowIfCancellationRequested();

        EnsureAccess(feature, forceLicenseRefresh: true);

        cancellationToken.ThrowIfCancellationRequested();

        return await operation(cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(
        AppFeature feature,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        cancellationToken.ThrowIfCancellationRequested();

        EnsureAccess(feature, forceLicenseRefresh: true);

        cancellationToken.ThrowIfCancellationRequested();

        await operation(cancellationToken).ConfigureAwait(false);
    }

    private LicensePlan SafeGetEffectivePlan()
    {
        try
        {
            return _licenseService.GetEffectivePlan();
        }
        catch
        {
            return LicensePlan.Free;
        }
    }

    private static string GetRoleText(UserRole role)
    {
        return role switch
        {
            UserRole.Guest => "مهمان",
            UserRole.Student => "دانشجو",
            UserRole.Teacher => "استاد",
            UserRole.UniversityAdmin => "مدیر دانشگاه",
            UserRole.SystemAdmin => "مدیر سیستم",
            _ => "نامشخص"
        };
    }
}

