using Hayt.Licensing.Services;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سیاست مرکزی دسترسی برنامه.
/// این کلاس مشخص می‌کند هر قابلیت چه Role و چه سطح License می‌خواهد.
/// </summary>
internal static class AppAccessPolicy
{
    public static string GetTitle(AppFeature feature)
    {
        return feature switch
        {
            AppFeature.None => "قابلیت عمومی",

            AppFeature.ViewDashboard => "مشاهده داشبورد",
            AppFeature.ViewLessons => "مشاهده درس‌ها",
            AppFeature.ViewBasicProgress => "مشاهده پیشرفت پایه",
            AppFeature.UseOfflineTutor => "استفاده از مربی آفلاین",
            AppFeature.UseBasicQuiz => "استفاده از آزمون پایه",

            AppFeature.ManageLessons => "مدیریت درس‌ها",
            AppFeature.AddLesson => "افزودن درس",
            AppFeature.EditLesson => "ویرایش درس",
            AppFeature.DeleteLesson => "حذف درس",
            AppFeature.UploadLessonAudio => "بارگذاری صوت درس",
            AppFeature.UploadLessonVideo => "بارگذاری ویدئوی درس",
            AppFeature.RecordLessonAudio => "ضبط صوت درس",
            AppFeature.ManageLessonFiles => "مدیریت فایل‌های درس",

            AppFeature.UseRealAITutor => "مربی هوشمند واقعی",
            AppFeature.GenerateAISummary => "خلاصه‌سازی هوشمند",
            AppFeature.GenerateAIQuiz => "تولید آزمون هوشمند",
            AppFeature.ViewAdvancedAnalytics => "تحلیل پیشرفته",
            AppFeature.ExportProfessionalReport => "خروجی حرفه‌ای",

            AppFeature.UseCloudSync => "همگام‌سازی ابری",
            AppFeature.UseOnlineBackup => "پشتیبان‌گیری آنلاین",
            AppFeature.UseMultiDeviceSync => "همگام‌سازی چنددستگاهی",

            AppFeature.ManageUsers => "مدیریت کاربران",
            AppFeature.ManageUniversity => "مدیریت دانشگاه",
            AppFeature.ViewOrganizationReports => "گزارش‌های سازمانی",
            AppFeature.ManageBulkLicenses => "مدیریت گروهی لایسنس",

            AppFeature.ManageApplicationSettings => "مدیریت تنظیمات برنامه",
            AppFeature.ManageLicense => "مدیریت لایسنس",
            AppFeature.ViewSecurityDiagnostics => "مشاهده عیب‌یابی امنیتی",

            _ => feature.ToString()
        };
    }

    public static bool IsKnownFeature(AppFeature feature)
    {
        return feature != AppFeature.None &&
               System.Enum.IsDefined(typeof(AppFeature), feature);
    }

    public static bool IsRoleAllowed(UserRole role, AppFeature feature)
    {
        if (!IsKnownFeature(feature))
        {
            return false;
        }

        if (role == UserRole.SystemAdmin)
        {
            return true;
        }

        return feature switch
        {
            // عمومی
            AppFeature.ViewDashboard => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.ViewLessons => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.ViewBasicProgress => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.UseOfflineTutor => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.UseBasicQuiz => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,

            // مدیریت درس: استاد و مدیر
            AppFeature.ManageLessons => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.AddLesson => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.EditLesson => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.DeleteLesson => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.UploadLessonAudio => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.UploadLessonVideo => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.RecordLessonAudio => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.ManageLessonFiles => role is UserRole.Teacher or UserRole.UniversityAdmin,

            // AI و گزارش
            AppFeature.UseRealAITutor => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.GenerateAISummary => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.GenerateAIQuiz => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.ViewAdvancedAnalytics => role is UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.ExportProfessionalReport => role is UserRole.Teacher or UserRole.UniversityAdmin,

            // Cloud
            AppFeature.UseCloudSync => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.UseOnlineBackup => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,
            AppFeature.UseMultiDeviceSync => role is UserRole.Student or UserRole.Teacher or UserRole.UniversityAdmin,

            // دانشگاه و کاربران
            AppFeature.ManageUsers => role is UserRole.UniversityAdmin,
            AppFeature.ManageUniversity => role is UserRole.UniversityAdmin,
            AppFeature.ViewOrganizationReports => role is UserRole.UniversityAdmin,
            AppFeature.ManageBulkLicenses => role is UserRole.UniversityAdmin,

            // سیستم
            AppFeature.ManageApplicationSettings => role is UserRole.UniversityAdmin,
            AppFeature.ManageLicense => role is UserRole.UniversityAdmin,
            AppFeature.ViewSecurityDiagnostics => role is UserRole.UniversityAdmin,

            _ => false
        };
    }

    public static PremiumFeature? GetRequiredPremiumFeature(AppFeature feature)
    {
        return feature switch
        {
            AppFeature.UseOfflineTutor => PremiumFeature.OfflineAITutor,
            AppFeature.ViewBasicProgress => PremiumFeature.BasicReports,
            AppFeature.UseBasicQuiz => PremiumFeature.BasicQuiz,

            AppFeature.UploadLessonAudio => PremiumFeature.ProfessionalExport,
            AppFeature.UploadLessonVideo => PremiumFeature.ProfessionalExport,
            AppFeature.RecordLessonAudio => PremiumFeature.ProfessionalExport,
            AppFeature.ManageLessonFiles => PremiumFeature.ProfessionalExport,

            AppFeature.UseRealAITutor => PremiumFeature.RealAITutor,
            AppFeature.GenerateAISummary => PremiumFeature.AISummarization,
            AppFeature.GenerateAIQuiz => PremiumFeature.AIQuizGeneration,
            AppFeature.ViewAdvancedAnalytics => PremiumFeature.AdvancedReports,
            AppFeature.ExportProfessionalReport => PremiumFeature.ProfessionalExport,

            AppFeature.UseCloudSync => PremiumFeature.CloudSync,
            AppFeature.UseOnlineBackup => PremiumFeature.OnlineBackup,
            AppFeature.UseMultiDeviceSync => PremiumFeature.MultiDeviceSync,

            AppFeature.ManageUsers => PremiumFeature.EnterpriseManagement,
            AppFeature.ManageUniversity => PremiumFeature.EnterpriseManagement,
            AppFeature.ViewOrganizationReports => PremiumFeature.OrganizationReports,
            AppFeature.ManageBulkLicenses => PremiumFeature.BulkLicenseManagement,

            _ => null
        };
    }

    public static bool RequiresLicense(AppFeature feature)
    {
        return GetRequiredPremiumFeature(feature).HasValue;
    }
}

