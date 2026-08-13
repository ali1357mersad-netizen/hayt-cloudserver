using Hayt.Licensing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس نهایی دسترسی برنامه.
/// این سرویس Role و License را با هم ترکیب می‌کند.
/// </summary>
public interface IAppAccessService
{
    AppAccessDecision CheckAccess(
        AppFeature feature,
        bool forceLicenseRefresh = false);

    bool CanAccess(
        AppFeature feature,
        bool forceLicenseRefresh = false);

    void EnsureAccess(
        AppFeature feature,
        bool forceLicenseRefresh = true);

    T Execute<T>(
        AppFeature feature,
        Func<T> operation);

    void Execute(
        AppFeature feature,
        Action operation);

    Task<T> ExecuteAsync<T>(
        AppFeature feature,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(
        AppFeature feature,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}

