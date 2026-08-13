using Hayt.Licensing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// درگاه مرکزی و اجباری دسترسی به قابلیت‌های محافظت‌شده.
/// </summary>
public interface IPremiumAccessService
{
    event EventHandler? AccessStateChanged;

    FeatureAccessDecision CheckAccess(
        PremiumFeature feature,
        bool forceRefresh = false);

    bool CanAccess(
        PremiumFeature feature,
        bool forceRefresh = false);

    void EnsureAccess(
        PremiumFeature feature,
        bool forceRefresh = true);

    void EnsurePremiumAccess(bool forceRefresh = true);

    T Execute<T>(
        PremiumFeature feature,
        Func<T> operation);

    void Execute(
        PremiumFeature feature,
        Action operation);

    Task<T> ExecuteAsync<T>(
        PremiumFeature feature,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(
        PremiumFeature feature,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    void Invalidate();
}

