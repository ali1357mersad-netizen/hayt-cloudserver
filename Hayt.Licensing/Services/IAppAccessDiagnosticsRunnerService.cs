using Hayt.Licensing.Services;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس اجرای کامل Diagnostics.
/// </summary>
public interface IAppAccessDiagnosticsRunnerService
{
    AppAccessDiagnosticsRunResult RunAndSave();

    AppAccessDiagnosticsRunResult RunAndSave(string rootDirectory);

    string RunAndSaveTextSummary();

    string RunAndSaveTextSummary(string rootDirectory);
}

