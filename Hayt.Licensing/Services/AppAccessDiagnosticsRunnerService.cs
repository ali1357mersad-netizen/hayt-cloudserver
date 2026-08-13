using Hayt.Licensing.Services;
using System;
using System.Text;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// Runner نهایی Diagnostics.
/// این سرویس برای دکمه UI، تست سریع، لاگ و ابزارهای داخلی مناسب است.
/// </summary>
public sealed class AppAccessDiagnosticsRunnerService : IAppAccessDiagnosticsRunnerService
{
    private readonly IAppAccessDiagnosticsFileService _fileService;

    public AppAccessDiagnosticsRunnerService()
        : this(AppAccessRuntime.DiagnosticsFileService)
    {
    }

    public AppAccessDiagnosticsRunnerService(
        IAppAccessDiagnosticsFileService fileService)
    {
        _fileService = fileService ??
            throw new ArgumentNullException(nameof(fileService));
    }

    public AppAccessDiagnosticsRunResult RunAndSave()
    {
        try
        {
            AppAccessRuntime.EnsureInitialized();

            AppAccessDiagnosticsSaveResult saveResult =
                _fileService.SaveReport();

            return AppAccessDiagnosticsRunResult.Success(saveResult);
        }
        catch (Exception ex)
        {
            return AppAccessDiagnosticsRunResult.Fail(
                "خطا هنگام اجرای Diagnostics: " + ex.Message,
                ex);
        }
    }

    public AppAccessDiagnosticsRunResult RunAndSave(string rootDirectory)
    {
        try
        {
            AppAccessRuntime.EnsureInitialized();

            AppAccessDiagnosticsSaveResult saveResult =
                _fileService.SaveReport(rootDirectory);

            return AppAccessDiagnosticsRunResult.Success(saveResult);
        }
        catch (Exception ex)
        {
            return AppAccessDiagnosticsRunResult.Fail(
                "خطا هنگام اجرای Diagnostics: " + ex.Message,
                ex);
        }
    }

    public string RunAndSaveTextSummary()
    {
        return ToTextSummary(RunAndSave());
    }

    public string RunAndSaveTextSummary(string rootDirectory)
    {
        return ToTextSummary(RunAndSave(rootDirectory));
    }

    private static string ToTextSummary(
        AppAccessDiagnosticsRunResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("========================================");
        sb.AppendLine("HAYT ACCESS DIAGNOSTICS RUN RESULT");
        sb.AppendLine("========================================");
        sb.AppendLine($"Status             : {result.StatusText}");
        sb.AppendLine($"Succeeded          : {result.Succeeded}");
        sb.AppendLine($"Diagnostics Passed : {result.DiagnosticsPassed}");
        sb.AppendLine($"Report Saved       : {result.ReportSaved}");
        sb.AppendLine($"Message            : {result.Message}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(result.ReportDirectory))
        {
            sb.AppendLine($"Report Directory   : {result.ReportDirectory}");
        }

        if (!string.IsNullOrWhiteSpace(result.TextReportPath))
        {
            sb.AppendLine($"Text Report        : {result.TextReportPath}");
        }

        if (!string.IsNullOrWhiteSpace(result.JsonReportPath))
        {
            sb.AppendLine($"Json Report        : {result.JsonReportPath}");
        }

        if (!string.IsNullOrWhiteSpace(result.SummaryPath))
        {
            sb.AppendLine($"Summary            : {result.SummaryPath}");
        }

        if (result.Report is not null)
        {
            sb.AppendLine();
            sb.AppendLine("REPORT SUMMARY");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Overall Passed     : {result.Report.OverallPassed}");
            sb.AppendLine($"Effective Plan     : {result.Report.EffectivePlan}");
            sb.AppendLine($"Current Role       : {result.Report.CurrentRole}");
            sb.AppendLine($"Role Tests         : {result.Report.PassedRoleTests}/{result.Report.TotalRoleTests}");
            sb.AppendLine($"Checks             : {result.Report.PassedChecks}/{result.Report.TotalChecks}");
            sb.AppendLine($"Failed Checks      : {result.Report.FailedChecks}");
        }

        if (result.Exception is not null)
        {
            sb.AppendLine();
            sb.AppendLine("EXCEPTION");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine(result.Exception.ToString());
        }

        sb.AppendLine("========================================");

        return sb.ToString();
    }
}

