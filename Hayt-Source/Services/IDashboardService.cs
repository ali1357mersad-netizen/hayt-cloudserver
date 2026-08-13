using System.Threading;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

/// <summary>
/// قرارداد دسترسی به داده‌های داشبورد تحلیلی کاربر جاری.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Snapshot داشبورد را برای کاربر جاری تولید می‌کند.
    /// </summary>
    Task<DashboardSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}

