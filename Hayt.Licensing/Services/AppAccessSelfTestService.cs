using Hayt.Licensing.Services;
using System;
using System.Collections.Generic;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// Self-Test برای بررسی Role + License Access Layer.
/// این تست‌ها بدون وابستگی به UI اجرا می‌شوند و هدفشان جلوگیری از شکست پنهان Runtime است.
/// </summary>
public sealed class AppAccessSelfTestService : IAppAccessSelfTestService
{
    private readonly IRoleAccessService _roleAccessService;
    private readonly IAppAccessService _appAccessService;
    private readonly ILicenseService _licenseService;

    public AppAccessSelfTestService()
        : this(
            AppAccessRuntime.RoleAccessService,
            AppAccessRuntime.AppAccessService,
            AppAccessRuntime.LicenseService)
    {
    }

    public AppAccessSelfTestService(
        IRoleAccessService roleAccessService,
        IAppAccessService appAccessService,
        ILicenseService licenseService)
    {
        _roleAccessService = roleAccessService ??
            throw new ArgumentNullException(nameof(roleAccessService));

        _appAccessService = appAccessService ??
            throw new ArgumentNullException(nameof(appAccessService));

        _licenseService = licenseService ??
            throw new ArgumentNullException(nameof(licenseService));
    }

    public IReadOnlyList<AppAccessSelfTestResult> RunAll()
    {
        var results = new List<AppAccessSelfTestResult>
        {
            RunForRole(UserRole.Guest),
            RunForRole(UserRole.Student),
            RunForRole(UserRole.Teacher),
            RunForRole(UserRole.UniversityAdmin),
            RunForRole(UserRole.SystemAdmin)
        };

        return results;
    }

    public AppAccessSelfTestResult RunForRole(UserRole role)
    {
        _roleAccessService.SetCurrentRole(role);

        LicensePlan plan = SafeGetEffectivePlan();

        var checks = new List<AppAccessSelfTestCheck>
        {
            Check(role, AppFeature.ViewDashboard, Expected(role, AppFeature.ViewDashboard)),
            Check(role, AppFeature.ViewLessons, Expected(role, AppFeature.ViewLessons)),
            Check(role, AppFeature.ViewBasicProgress, Expected(role, AppFeature.ViewBasicProgress)),
            Check(role, AppFeature.UseOfflineTutor, Expected(role, AppFeature.UseOfflineTutor)),
            Check(role, AppFeature.UseBasicQuiz, Expected(role, AppFeature.UseBasicQuiz)),

            Check(role, AppFeature.ManageLessons, Expected(role, AppFeature.ManageLessons)),
            Check(role, AppFeature.AddLesson, Expected(role, AppFeature.AddLesson)),
            Check(role, AppFeature.EditLesson, Expected(role, AppFeature.EditLesson)),
            Check(role, AppFeature.DeleteLesson, Expected(role, AppFeature.DeleteLesson)),
            Check(role, AppFeature.RecordLessonAudio, Expected(role, AppFeature.RecordLessonAudio)),
            Check(role, AppFeature.UploadLessonAudio, Expected(role, AppFeature.UploadLessonAudio)),
            Check(role, AppFeature.UploadLessonVideo, Expected(role, AppFeature.UploadLessonVideo)),

            Check(role, AppFeature.UseRealAITutor, Expected(role, AppFeature.UseRealAITutor)),
            Check(role, AppFeature.GenerateAISummary, Expected(role, AppFeature.GenerateAISummary)),
            Check(role, AppFeature.GenerateAIQuiz, Expected(role, AppFeature.GenerateAIQuiz)),
            Check(role, AppFeature.ViewAdvancedAnalytics, Expected(role, AppFeature.ViewAdvancedAnalytics)),

            Check(role, AppFeature.UseCloudSync, Expected(role, AppFeature.UseCloudSync)),
            Check(role, AppFeature.UseOnlineBackup, Expected(role, AppFeature.UseOnlineBackup)),

            Check(role, AppFeature.ManageUsers, Expected(role, AppFeature.ManageUsers)),
            Check(role, AppFeature.ManageUniversity, Expected(role, AppFeature.ManageUniversity)),
            Check(role, AppFeature.ViewOrganizationReports, Expected(role, AppFeature.ViewOrganizationReports)),
            Check(role, AppFeature.ManageBulkLicenses, Expected(role, AppFeature.ManageBulkLicenses)),

            Check(role, AppFeature.ManageApplicationSettings, Expected(role, AppFeature.ManageApplicationSettings)),
            Check(role, AppFeature.ManageLicense, Expected(role, AppFeature.ManageLicense)),
            Check(role, AppFeature.ViewSecurityDiagnostics, Expected(role, AppFeature.ViewSecurityDiagnostics))
        };

        return AppAccessSelfTestResult.FromChecks(
            $"Access self-test for {role}",
            role,
            plan,
            checks);
    }

    private AppAccessSelfTestCheck Check(
        UserRole role,
        AppFeature feature,
        bool expectedAllowed)
    {
        try
        {
            AppAccessDecision decision =
                _appAccessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessSelfTestCheck.Create(
                feature,
                decision.FeatureTitle,
                expectedAllowed,
                decision.IsAllowed,
                decision.ReasonCode,
                decision.Message);
        }
        catch (Exception ex)
        {
            return AppAccessSelfTestCheck.Create(
                feature,
                feature.ToString(),
                expectedAllowed,
                actualAllowed: false,
                reasonCode: "SELF_TEST_EXCEPTION",
                message: $"Self-test exception for role {role}: {ex.Message}");
        }
    }

    /// <summary>
    /// انتظار تست بر اساس وضعیت فعلی License و Role محاسبه می‌شود.
    /// این باعث می‌شود Self-Test هم در Free، هم Trial و هم Premium قابل استفاده باشد.
    /// </summary>
    private bool Expected(UserRole role, AppFeature feature)
    {
        if (!AppAccessPolicy.IsKnownFeature(feature))
        {
            return false;
        }

        bool roleAllowed = AppAccessPolicy.IsRoleAllowed(role, feature);

        if (!roleAllowed)
        {
            return false;
        }

        PremiumFeature? requiredPremiumFeature =
            AppAccessPolicy.GetRequiredPremiumFeature(feature);

        if (!requiredPremiumFeature.HasValue)
        {
            return true;
        }

        try
        {
            IPremiumAccessService premiumAccessService =
                AppAccessRuntime.PremiumAccessService;

            return premiumAccessService.CanAccess(
                requiredPremiumFeature.Value,
                forceRefresh: false);
        }
        catch
        {
            return false;
        }
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
}

