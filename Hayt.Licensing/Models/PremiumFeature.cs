using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// فهرست مرکزی قابلیت‌هایی که ممکن است به License Gate نیاز داشته باشند.
/// برای افزودن قابلیت جدید، ابتدا آن را در این enum و سپس در
/// PremiumFeaturePolicy ثبت کنید.
/// </summary>
public enum PremiumFeature
{
    None = 0,

    // Free features
    OfflineAITutor = 10,
    BasicNotes = 11,
    BasicReports = 12,
    BasicQuiz = 13,

    // Premium features
    RealAITutor = 100,
    AISummarization = 101,
    AIQuizGeneration = 102,
    AdvancedReports = 103,
    ProfessionalExport = 104,
    UnlimitedNotes = 105,
    AdvancedGoals = 106,
    AdvancedAchievements = 107,
    PremiumThemes = 108,

    // A real paid license is required even during Trial.
    CloudSync = 200,
    OnlineBackup = 201,
    MultiDeviceSync = 202,

    // Enterprise-only capabilities
    EnterpriseManagement = 300,
    OrganizationReports = 301,
    BulkLicenseManagement = 302
}

