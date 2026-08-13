using Hayt.Licensing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// اجرای امن عملیات برای UI/WPF.
/// این سرویس Exceptionهای دسترسی را به Result قابل نمایش تبدیل می‌کند.
/// </summary>
public interface IAppAccessGuard
{
    AppAccessExecutionResult TryExecute(
        AppFeature feature,
        Action operation);

    AppAccessExecutionResult<T> TryExecute<T>(
        AppFeature feature,
        Func<T> operation);

    Task<AppAccessExecutionResult> TryExecuteAsync(
        AppFeature feature,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    Task<AppAccessExecutionResult<T>> TryExecuteAsync<T>(
        AppFeature feature,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}

