using Hayt.Licensing.Services;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

internal enum PremiumFeatureTier
{
    Free = 0,
    Premium = 1,
    PaidLicenseOnly = 2,
    Enterprise = 3
}

/// <summary>
/// سیاست مرکزی قابلیت‌ها.
/// هیچ تصمیم دسترسی نباید با پراکندگی شرط‌های plan در UI گرفته شود.
/// </summary>
internal static class PremiumFeaturePolicy
{
    public static PremiumFeatureTier GetTier(PremiumFeature feature)
    {
        return feature switch
        {
            PremiumFeature.None => PremiumFeatureTier.Free,

            PremiumFeature.OfflineAITutor => PremiumFeatureTier.Free,
            PremiumFeature.BasicNotes => PremiumFeatureTier.Free,
            PremiumFeature.BasicReports => PremiumFeatureTier.Free,
            PremiumFeature.BasicQuiz => PremiumFeatureTier.Free,

            PremiumFeature.RealAITutor => PremiumFeatureTier.Premium,
            PremiumFeature.AISummarization => PremiumFeatureTier.Premium,
            PremiumFeature.AIQuizGeneration => PremiumFeatureTier.Premium,
            PremiumFeature.AdvancedReports => PremiumFeatureTier.Premium,
            PremiumFeature.ProfessionalExport => PremiumFeatureTier.Premium,
            PremiumFeature.UnlimitedNotes => PremiumFeatureTier.Premium,
            PremiumFeature.AdvancedGoals => PremiumFeatureTier.Premium,
            PremiumFeature.AdvancedAchievements => PremiumFeatureTier.Premium,
            PremiumFeature.PremiumThemes => PremiumFeatureTier.Premium,

            PremiumFeature.CloudSync => PremiumFeatureTier.PaidLicenseOnly,
            PremiumFeature.OnlineBackup => PremiumFeatureTier.PaidLicenseOnly,
            PremiumFeature.MultiDeviceSync => PremiumFeatureTier.PaidLicenseOnly,

            PremiumFeature.EnterpriseManagement => PremiumFeatureTier.Enterprise,
            PremiumFeature.OrganizationReports => PremiumFeatureTier.Enterprise,
            PremiumFeature.BulkLicenseManagement => PremiumFeatureTier.Enterprise,

            _ => PremiumFeatureTier.PaidLicenseOnly
        };
    }

    public static string GetTitle(PremiumFeature feature)
    {
        return feature switch
        {
            PremiumFeature.None => "قابلیت عمومی",
            PremiumFeature.OfflineAITutor => "مربی هوشمند آفلاین",
            PremiumFeature.BasicNotes => "یادداشت‌های پایه",
            PremiumFeature.BasicReports => "گزارش‌های پایه",
            PremiumFeature.BasicQuiz => "آزمون پایه",
            PremiumFeature.RealAITutor => "مربی هوشمند واقعی",
            PremiumFeature.AISummarization => "خلاصه‌سازی هوشمند",
            PremiumFeature.AIQuizGeneration => "تولید آزمون با هوش مصنوعی",
            PremiumFeature.AdvancedReports => "گزارش‌های پیشرفته",
            PremiumFeature.ProfessionalExport => "خروجی حرفه‌ای",
            PremiumFeature.UnlimitedNotes => "یادداشت نامحدود",
            PremiumFeature.AdvancedGoals => "اهداف پیشرفته",
            PremiumFeature.AdvancedAchievements => "دستاوردهای پیشرفته",
            PremiumFeature.PremiumThemes => "تم‌های Premium",
            PremiumFeature.CloudSync => "همگام‌سازی ابری",
            PremiumFeature.OnlineBackup => "پشتیبان‌گیری آنلاین",
            PremiumFeature.MultiDeviceSync => "همگام‌سازی چنددستگاهی",
            PremiumFeature.EnterpriseManagement => "مدیریت سازمانی",
            PremiumFeature.OrganizationReports => "گزارش‌های سازمانی",
            PremiumFeature.BulkLicenseManagement => "مدیریت گروهی لایسنس",
            _ => feature.ToString()
        };
    }
}

