using Hayt.Licensing.Services;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// ذخیره‌کننده گزارش Diagnostics.
/// خروجی‌ها:
/// - TXT برای خواندن سریع
/// - JSON برای UI یا ابزارهای بعدی
/// - SUMMARY برای بررسی سریع وضعیت
/// </summary>
public sealed class AppAccessDiagnosticsFileService : IAppAccessDiagnosticsFileService
{
    private readonly IAppAccessDiagnosticsService _diagnosticsService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public AppAccessDiagnosticsFileService()
        : this(AppAccessRuntime.DiagnosticsService)
    {
    }

    public AppAccessDiagnosticsFileService(
        IAppAccessDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService ??
            throw new ArgumentNullException(nameof(diagnosticsService));
    }

    public AppAccessDiagnosticsSaveResult SaveReport()
    {
        string baseDirectory = AppContext.BaseDirectory;

        string? root = FindProjectRoot(baseDirectory);

        if (string.IsNullOrWhiteSpace(root))
        {
            root = baseDirectory;
        }

        return SaveReport(root);
    }

    public AppAccessDiagnosticsSaveResult SaveReport(string rootDirectory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                rootDirectory = AppContext.BaseDirectory;
            }

            string normalizedRoot =
                Path.GetFullPath(rootDirectory);

            string stamp =
                DateTime.Now.ToString("yyyyMMdd-HHmmss");

            string reportDirectory = Path.Combine(
                normalizedRoot,
                "_SafeBackups",
                "AccessDiagnostics",
                $"AccessDiagnostics-{stamp}");

            Directory.CreateDirectory(reportDirectory);

            string textPath = Path.Combine(
                reportDirectory,
                "AccessDiagnosticsReport.txt");

            string jsonPath = Path.Combine(
                reportDirectory,
                "AccessDiagnosticsReport.json");

            string summaryPath = Path.Combine(
                reportDirectory,
                "SUMMARY.txt");

            AppAccessDiagnosticsReport report =
                _diagnosticsService.CreateReport();

            string text = report.ToText();

            string json = JsonSerializer.Serialize(
                report,
                _jsonOptions);

            string summary = CreateSummaryText(
                report,
                reportDirectory,
                textPath,
                jsonPath);

            WriteUtf8NoBom(textPath, text);
            WriteUtf8NoBom(jsonPath, json);
            WriteUtf8NoBom(summaryPath, summary);

            return AppAccessDiagnosticsSaveResult.Success(
                reportDirectory,
                textPath,
                jsonPath,
                summaryPath,
                report);
        }
        catch (Exception ex)
        {
            return AppAccessDiagnosticsSaveResult.Fail(
                "خطا هنگام ذخیره گزارش عیب‌یابی دسترسی: " + ex.Message,
                ex);
        }
    }

    private static string CreateSummaryText(
        AppAccessDiagnosticsReport report,
        string reportDirectory,
        string textPath,
        string jsonPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("========================================");
        sb.AppendLine("HAYT ACCESS DIAGNOSTICS SUMMARY");
        sb.AppendLine("========================================");
        sb.AppendLine($"Created UTC       : {report.CreatedAtUtc:O}");
        sb.AppendLine($"Status            : {report.StatusText}");
        sb.AppendLine($"Overall Passed    : {report.OverallPassed}");
        sb.AppendLine($"Effective Plan    : {report.EffectivePlan}");
        sb.AppendLine($"Current Role      : {report.CurrentRole}");
        sb.AppendLine($"Role Tests        : {report.PassedRoleTests}/{report.TotalRoleTests}");
        sb.AppendLine($"Checks            : {report.PassedChecks}/{report.TotalChecks}");
        sb.AppendLine($"Failed Checks     : {report.FailedChecks}");
        sb.AppendLine($"Summary           : {report.SummaryMessage}");
        sb.AppendLine();
        sb.AppendLine($"Report Directory  : {reportDirectory}");
        sb.AppendLine($"Text Report       : {textPath}");
        sb.AppendLine($"Json Report       : {jsonPath}");
        sb.AppendLine("========================================");

        return sb.ToString();
    }

    private static void WriteUtf8NoBom(
        string path,
        string content)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            path,
            content ?? string.Empty,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string? FindProjectRoot(string startDirectory)
    {
        try
        {
            DirectoryInfo? current = new DirectoryInfo(startDirectory);

            while (current is not null)
            {
                FileInfo[] projectFiles =
                    current.GetFiles("*.csproj");

                if (projectFiles.Length > 0)
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

