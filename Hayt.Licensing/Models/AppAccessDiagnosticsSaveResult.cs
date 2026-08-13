using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;

namespace Hayt.Licensing.Models;

/// <summary>
/// نتیجه ذخیره گزارش Diagnostics در فایل.
/// برای نمایش در UI یا Log مناسب است.
/// </summary>
public sealed class AppAccessDiagnosticsSaveResult
{
    public bool Succeeded { get; init; }

    public string ReportDirectory { get; init; } = string.Empty;

    public string TextReportPath { get; init; } = string.Empty;

    public string JsonReportPath { get; init; } = string.Empty;

    public string SummaryPath { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public Exception? Exception { get; init; }

    public AppAccessDiagnosticsReport? Report { get; init; }

    public static AppAccessDiagnosticsSaveResult Success(
        string reportDirectory,
        string textReportPath,
        string jsonReportPath,
        string summaryPath,
        AppAccessDiagnosticsReport report)
    {
        return new AppAccessDiagnosticsSaveResult
        {
            Succeeded = true,
            ReportDirectory = reportDirectory,
            TextReportPath = textReportPath,
            JsonReportPath = jsonReportPath,
            SummaryPath = summaryPath,
            Message = "گزارش عیب‌یابی دسترسی با موفقیت ذخیره شد.",
            Exception = null,
            Report = report
        };
    }

    public static AppAccessDiagnosticsSaveResult Fail(
        string message,
        Exception exception)
    {
        return new AppAccessDiagnosticsSaveResult
        {
            Succeeded = false,
            ReportDirectory = string.Empty,
            TextReportPath = string.Empty,
            JsonReportPath = string.Empty,
            SummaryPath = string.Empty,
            Message = message,
            Exception = exception,
            Report = null
        };
    }
}

