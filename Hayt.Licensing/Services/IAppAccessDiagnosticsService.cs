using Hayt.Licensing.Services;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس ساخت گزارش کامل از وضعیت Role + License Access.
/// </summary>
public interface IAppAccessDiagnosticsService
{
    AppAccessDiagnosticsReport CreateReport();

    string CreateTextReport();
}

