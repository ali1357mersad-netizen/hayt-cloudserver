using Hayt.Licensing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// Guard امن برای استفاده در ViewModel یا code-behind.
/// هدف: جلوگیری از Crash و تبدیل خطای دسترسی به پیام قابل نمایش.
/// </summary>
public sealed class AppAccessGuard : IAppAccessGuard
{
    private readonly IAppAccessService _accessService;

    public AppAccessGuard(IAppAccessService accessService)
    {
        _accessService = accessService ??
            throw new ArgumentNullException(nameof(accessService));
    }

    public AppAccessExecutionResult TryExecute(
        AppFeature feature,
        Action operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        try
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: true);

            if (!decision.IsAllowed)
            {
                return AppAccessExecutionResult.Denied(feature, decision);
            }

            operation();

            return AppAccessExecutionResult.Success(
                feature,
                decision,
                "عملیات با موفقیت انجام شد.");
        }
        catch (AppAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult.Denied(feature, decision);
        }
        catch (PremiumAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult.Denied(feature, decision);
        }
        catch (OperationCanceledException ex)
        {
            return AppAccessExecutionResult.Error(
                feature,
                "عملیات لغو شد.",
                ex);
        }
        catch (Exception ex)
        {
            return AppAccessExecutionResult.Error(
                feature,
                "خطا هنگام اجرای عملیات: " + ex.Message,
                ex);
        }
    }

    public AppAccessExecutionResult<T> TryExecute<T>(
        AppFeature feature,
        Func<T> operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        try
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: true);

            if (!decision.IsAllowed)
            {
                return AppAccessExecutionResult<T>.Denied(feature, decision);
            }

            T value = operation();

            return AppAccessExecutionResult<T>.Success(
                feature,
                decision,
                value,
                "عملیات با موفقیت انجام شد.");
        }
        catch (AppAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult<T>.Denied(feature, decision);
        }
        catch (PremiumAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult<T>.Denied(feature, decision);
        }
        catch (OperationCanceledException ex)
        {
            return AppAccessExecutionResult<T>.Error(
                feature,
                "عملیات لغو شد.",
                ex);
        }
        catch (Exception ex)
        {
            return AppAccessExecutionResult<T>.Error(
                feature,
                "خطا هنگام اجرای عملیات: " + ex.Message,
                ex);
        }
    }

    public async Task<AppAccessExecutionResult> TryExecuteAsync(
        AppFeature feature,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: true);

            if (!decision.IsAllowed)
            {
                return AppAccessExecutionResult.Denied(feature, decision);
            }

            await operation(cancellationToken).ConfigureAwait(false);

            return AppAccessExecutionResult.Success(
                feature,
                decision,
                "عملیات با موفقیت انجام شد.");
        }
        catch (AppAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult.Denied(feature, decision);
        }
        catch (PremiumAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult.Denied(feature, decision);
        }
        catch (OperationCanceledException ex)
        {
            return AppAccessExecutionResult.Error(
                feature,
                "عملیات لغو شد.",
                ex);
        }
        catch (Exception ex)
        {
            return AppAccessExecutionResult.Error(
                feature,
                "خطا هنگام اجرای عملیات: " + ex.Message,
                ex);
        }
    }

    public async Task<AppAccessExecutionResult<T>> TryExecuteAsync<T>(
        AppFeature feature,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: true);

            if (!decision.IsAllowed)
            {
                return AppAccessExecutionResult<T>.Denied(feature, decision);
            }

            T value = await operation(cancellationToken).ConfigureAwait(false);

            return AppAccessExecutionResult<T>.Success(
                feature,
                decision,
                value,
                "عملیات با موفقیت انجام شد.");
        }
        catch (AppAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult<T>.Denied(feature, decision);
        }
        catch (PremiumAccessDeniedException)
        {
            AppAccessDecision decision =
                _accessService.CheckAccess(feature, forceLicenseRefresh: false);

            return AppAccessExecutionResult<T>.Denied(feature, decision);
        }
        catch (OperationCanceledException ex)
        {
            return AppAccessExecutionResult<T>.Error(
                feature,
                "عملیات لغو شد.",
                ex);
        }
        catch (Exception ex)
        {
            return AppAccessExecutionResult<T>.Error(
                feature,
                "خطا هنگام اجرای عملیات: " + ex.Message,
                ex);
        }
    }
}

