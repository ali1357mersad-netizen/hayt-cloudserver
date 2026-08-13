using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

/// <summary>
/// نتیجه اجرای کامل Diagnostics.
/// این مدل برای UI، MessageBox، Log و دکمه تست مناسب است.
/// </summary>
public sealed class AppAccessDiagnosticsRunResult
{
    public bool Succeeded { get; init; }

    public bool DiagnosticsPassed { get; init; }

    public bool ReportSaved { get; init; }

    public string StatusText => Succeeded ? "PASS" : "FAIL";

    public string Message { get; init; } = string.Empty;

    public string ReportDirectory { get; init; } = string.Empty;

    public string TextReportPath { get; init; } = string.Empty;

    public string JsonReportPath { get; init; } = string.Empty;

    public string SummaryPath { get; init; } = string.Empty;

    public AppAccessDiagnosticsReport? Report { get; init; }

    public AppAccessDiagnosticsSaveResult? SaveResult { get; init; }

    public Exception? Exception { get; init; }

    public static AppAccessDiagnosticsRunResult Success(
        AppAccessDiagnosticsSaveResult saveResult)
    {
        bool diagnosticsPassed =
            saveResult.Report?.OverallPassed ?? false;

        return new AppAccessDiagnosticsRunResult
        {
            Succeeded = saveResult.Succeeded,
            DiagnosticsPassed = diagnosticsPassed,
            ReportSaved = saveResult.Succeeded,
            Message = saveResult.Succeeded
                ? diagnosticsPassed
                    ? "Diagnostics با موفقیت اجرا و ذخیره شد. همه تست‌ها PASS شدند."
                    : "Diagnostics اجرا و ذخیره شد، اما بعضی تست‌ها FAIL هستند. گزارش را بررسی کنید."
                : saveResult.Message,
            ReportDirectory = saveResult.ReportDirectory,
            TextReportPath = saveResult.TextReportPath,
            JsonReportPath = saveResult.JsonReportPath,
            SummaryPath = saveResult.SummaryPath,
            Report = saveResult.Report,
            SaveResult = saveResult,
            Exception = saveResult.Exception
        };
    }

    public static AppAccessDiagnosticsRunResult Fail(
        string message,
        Exception exception)
    {
        return new AppAccessDiagnosticsRunResult
        {
            Succeeded = false,
            DiagnosticsPassed = false,
            ReportSaved = false,
            Message = message,
            ReportDirectory = string.Empty,
            TextReportPath = string.Empty,
            JsonReportPath = string.Empty,
            SummaryPath = string.Empty,
            Report = null,
            SaveResult = null,
            Exception = exception
        };
    }
}

