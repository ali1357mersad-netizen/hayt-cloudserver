using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// قابلیت‌های سطح برنامه که ممکن است هم Role و هم License بخواهند.
/// این enum برای تصمیم نهایی دسترسی در کل برنامه استفاده می‌شود.
/// </summary>
public enum AppFeature
{
    None = 0,

    // عمومی
    ViewDashboard = 10,
    ViewLessons = 11,
    ViewBasicProgress = 12,
    UseOfflineTutor = 13,
    UseBasicQuiz = 14,

    // درس و محتوا
    ManageLessons = 100,
    AddLesson = 101,
    EditLesson = 102,
    DeleteLesson = 103,
    UploadLessonAudio = 104,
    UploadLessonVideo = 105,
    RecordLessonAudio = 106,
    ManageLessonFiles = 107,

    // هوش مصنوعی و گزارش
    UseRealAITutor = 200,
    GenerateAISummary = 201,
    GenerateAIQuiz = 202,
    ViewAdvancedAnalytics = 203,
    ExportProfessionalReport = 204,

    // Cloud
    UseCloudSync = 300,
    UseOnlineBackup = 301,
    UseMultiDeviceSync = 302,

    // کاربران و دانشگاه
    ManageUsers = 400,
    ManageUniversity = 401,
    ViewOrganizationReports = 402,
    ManageBulkLicenses = 403,

    // سیستم
    ManageApplicationSettings = 500,
    ManageLicense = 501,
    ViewSecurityDiagnostics = 502
}

