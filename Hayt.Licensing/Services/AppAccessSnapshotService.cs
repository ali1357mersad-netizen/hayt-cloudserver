using Hayt.Licensing.Services;
using System;
using System.Collections.Generic;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// تولید Snapshot از دسترسی‌ها برای نمایش در WPF.
/// این سرویس فقط وضعیت را می‌خواند و عملیات حساس انجام نمی‌دهد.
/// </summary>
public sealed class AppAccessSnapshotService : IAppAccessSnapshotService
{
    private readonly IAppAccessService _accessService;

    public AppAccessSnapshotService(IAppAccessService accessService)
    {
        _accessService = accessService ??
            throw new ArgumentNullException(nameof(accessService));
    }

    public IReadOnlyList<AppAccessStatusItem> GetAll()
    {
        var result = new List<AppAccessStatusItem>();

        foreach (AppFeature feature in Enum.GetValues<AppFeature>())
        {
            if (feature == AppFeature.None)
            {
                continue;
            }

            result.Add(ToStatusItem(
                _accessService.CheckAccess(feature)));
        }

        return result;
    }

    public IReadOnlyList<AppAccessStatusItem> GetTeacherPanelItems()
    {
        return GetSelected(
            AppFeature.ViewLessons,
            AppFeature.ManageLessons,
            AppFeature.AddLesson,
            AppFeature.EditLesson,
            AppFeature.DeleteLesson,
            AppFeature.RecordLessonAudio,
            AppFeature.UploadLessonAudio,
            AppFeature.UploadLessonVideo,
            AppFeature.ManageLessonFiles,
            AppFeature.GenerateAIQuiz,
            AppFeature.ViewAdvancedAnalytics,
            AppFeature.ExportProfessionalReport);
    }

    public IReadOnlyList<AppAccessStatusItem> GetStudentPanelItems()
    {
        return GetSelected(
            AppFeature.ViewDashboard,
            AppFeature.ViewLessons,
            AppFeature.ViewBasicProgress,
            AppFeature.UseOfflineTutor,
            AppFeature.UseBasicQuiz,
            AppFeature.UseRealAITutor,
            AppFeature.GenerateAISummary,
            AppFeature.UseCloudSync);
    }

    public IReadOnlyList<AppAccessStatusItem> GetAdminPanelItems()
    {
        return GetSelected(
            AppFeature.ManageUsers,
            AppFeature.ManageUniversity,
            AppFeature.ViewOrganizationReports,
            AppFeature.ManageBulkLicenses,
            AppFeature.ManageApplicationSettings,
            AppFeature.ManageLicense,
            AppFeature.ViewSecurityDiagnostics);
    }

    private IReadOnlyList<AppAccessStatusItem> GetSelected(
        params AppFeature[] features)
    {
        var result = new List<AppAccessStatusItem>();

        foreach (AppFeature feature in features)
        {
            result.Add(ToStatusItem(
                _accessService.CheckAccess(feature)));
        }

        return result;
    }

    private static AppAccessStatusItem ToStatusItem(
        AppAccessDecision decision)
    {
        return new AppAccessStatusItem
        {
            Feature = decision.Feature,
            FeatureTitle = decision.FeatureTitle,
            Role = decision.Role,
            EffectivePlan = decision.EffectivePlan,
            IsAllowed = decision.IsAllowed,
            RoleAllowed = decision.RoleAllowed,
            LicenseAllowed = decision.LicenseAllowed,
            RequiresUpgrade = decision.RequiresUpgrade,
            RequiresHigherRole = decision.RequiresHigherRole,
            Message = decision.Message,
            ReasonCode = decision.ReasonCode
        };
    }
}

