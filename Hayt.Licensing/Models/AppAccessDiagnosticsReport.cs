using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hayt.Licensing.Models;

/// <summary>
/// گزارش کامل عیب‌یابی لایه دسترسی.
/// این مدل برای ذخیره در فایل، نمایش در UI یا ارسال به لاگ داخلی مناسب است.
/// </summary>
public sealed class AppAccessDiagnosticsReport
{
    public string Title { get; init; } = "Hayt Access Diagnostics Report";

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public LicensePlan EffectivePlan { get; init; } = LicensePlan.Free;

    public UserRole CurrentRole { get; init; } = UserRole.Guest;

    public bool OverallPassed { get; init; }

    public int TotalRoleTests { get; init; }

    public int PassedRoleTests { get; init; }

    public int FailedRoleTests { get; init; }

    public int TotalChecks { get; init; }

    public int PassedChecks { get; init; }

    public int FailedChecks { get; init; }

    public string SummaryMessage { get; init; } = string.Empty;

    public IReadOnlyList<AppAccessSelfTestResult> RoleResults { get; init; } =
        new List<AppAccessSelfTestResult>();

    public IReadOnlyList<AppAccessStatusItem> AllFeatureSnapshot { get; init; } =
        new List<AppAccessStatusItem>();

    public IReadOnlyList<AppAccessStatusItem> TeacherPanelSnapshot { get; init; } =
        new List<AppAccessStatusItem>();

    public IReadOnlyList<AppAccessStatusItem> StudentPanelSnapshot { get; init; } =
        new List<AppAccessStatusItem>();

    public IReadOnlyList<AppAccessStatusItem> AdminPanelSnapshot { get; init; } =
        new List<AppAccessStatusItem>();

    public string StatusText => OverallPassed ? "PASS" : "FAIL";

    public static AppAccessDiagnosticsReport Create(
        LicensePlan effectivePlan,
        UserRole currentRole,
        IReadOnlyList<AppAccessSelfTestResult> roleResults,
        IReadOnlyList<AppAccessStatusItem> allFeatureSnapshot,
        IReadOnlyList<AppAccessStatusItem> teacherPanelSnapshot,
        IReadOnlyList<AppAccessStatusItem> studentPanelSnapshot,
        IReadOnlyList<AppAccessStatusItem> adminPanelSnapshot)
    {
        int totalRoleTests = roleResults.Count;
        int passedRoleTests = roleResults.Count(x => x.Passed);
        int failedRoleTests = totalRoleTests - passedRoleTests;

        int totalChecks = roleResults.Sum(x => x.TotalChecks);
        int passedChecks = roleResults.Sum(x => x.PassedChecks);
        int failedChecks = roleResults.Sum(x => x.FailedChecks);

        bool overallPassed =
            failedRoleTests == 0 &&
            failedChecks == 0;

        return new AppAccessDiagnosticsReport
        {
            CreatedAtUtc = DateTime.UtcNow,
            EffectivePlan = effectivePlan,
            CurrentRole = currentRole,
            OverallPassed = overallPassed,
            TotalRoleTests = totalRoleTests,
            PassedRoleTests = passedRoleTests,
            FailedRoleTests = failedRoleTests,
            TotalChecks = totalChecks,
            PassedChecks = passedChecks,
            FailedChecks = failedChecks,
            SummaryMessage = overallPassed
                ? "همه تست‌های دسترسی با موفقیت انجام شدند."
                : $"{failedChecks} بررسی دسترسی ناموفق بود.",
            RoleResults = roleResults,
            AllFeatureSnapshot = allFeatureSnapshot,
            TeacherPanelSnapshot = teacherPanelSnapshot,
            StudentPanelSnapshot = studentPanelSnapshot,
            AdminPanelSnapshot = adminPanelSnapshot
        };
    }

    public string ToText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("========================================");
        sb.AppendLine(Title);
        sb.AppendLine("========================================");
        sb.AppendLine($"Created UTC      : {CreatedAtUtc:O}");
        sb.AppendLine($"Status           : {StatusText}");
        sb.AppendLine($"Effective Plan   : {EffectivePlan}");
        sb.AppendLine($"Current Role     : {CurrentRole}");
        sb.AppendLine($"Role Tests       : {PassedRoleTests}/{TotalRoleTests}");
        sb.AppendLine($"Checks           : {PassedChecks}/{TotalChecks}");
        sb.AppendLine($"Failed Checks    : {FailedChecks}");
        sb.AppendLine($"Summary          : {SummaryMessage}");
        sb.AppendLine();

        sb.AppendLine("========================================");
        sb.AppendLine("ROLE SELF-TEST RESULTS");
        sb.AppendLine("========================================");

        foreach (AppAccessSelfTestResult roleResult in RoleResults)
        {
            sb.AppendLine();
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"{roleResult.TestName}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Role          : {roleResult.Role}");
            sb.AppendLine($"Plan          : {roleResult.EffectivePlan}");
            sb.AppendLine($"Status        : {roleResult.StatusText}");
            sb.AppendLine($"Checks        : {roleResult.PassedChecks}/{roleResult.TotalChecks}");
            sb.AppendLine($"Failed        : {roleResult.FailedChecks}");
            sb.AppendLine($"Message       : {roleResult.Message}");

            foreach (AppAccessSelfTestCheck check in roleResult.Checks)
            {
                string mark = check.Passed ? "PASS" : "FAIL";

                sb.AppendLine(
                    $"  [{mark}] {check.Feature} | Expected={check.ExpectedAllowed} | Actual={check.ActualAllowed} | Reason={check.ReasonCode} | {check.Message}");
            }
        }

        AppendSnapshot(sb, "CURRENT ROLE - ALL FEATURES SNAPSHOT", AllFeatureSnapshot);
        AppendSnapshot(sb, "TEACHER PANEL SNAPSHOT", TeacherPanelSnapshot);
        AppendSnapshot(sb, "STUDENT PANEL SNAPSHOT", StudentPanelSnapshot);
        AppendSnapshot(sb, "ADMIN PANEL SNAPSHOT", AdminPanelSnapshot);

        return sb.ToString();
    }

    private static void AppendSnapshot(
        StringBuilder sb,
        string title,
        IReadOnlyList<AppAccessStatusItem> items)
    {
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine(title);
        sb.AppendLine("========================================");

        foreach (AppAccessStatusItem item in items)
        {
            sb.AppendLine(
                $"{item.Feature} | {item.StatusText} | Role={item.Role} | Plan={item.EffectivePlan} | RoleAllowed={item.RoleAllowed} | LicenseAllowed={item.LicenseAllowed} | Reason={item.ReasonCode} | {item.Message}");
        }
    }
}

