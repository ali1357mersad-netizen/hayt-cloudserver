using Hayt.Licensing.Services;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس ذخیره گزارش Diagnostics در فایل.
/// </summary>
public interface IAppAccessDiagnosticsFileService
{
    AppAccessDiagnosticsSaveResult SaveReport();

    AppAccessDiagnosticsSaveResult SaveReport(string rootDirectory);
}

