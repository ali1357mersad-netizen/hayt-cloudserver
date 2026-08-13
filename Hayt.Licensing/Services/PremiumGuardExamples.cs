using Hayt.Licensing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// نمونه‌های آماده اتصال Gate به سرویس‌های پروژه.
/// این کلاس عملیات واقعی AI یا Cloud را پیاده‌سازی نمی‌کند؛
/// الگوی صحیح حفاظت در Service Layer را فراهم می‌کند.
/// </summary>
public sealed class PremiumGuardExamples
{
    private readonly IPremiumAccessService _premiumAccess;

    public PremiumGuardExamples()
        : this(new PremiumAccessService())
    {
    }

    public PremiumGuardExamples(
        IPremiumAccessService premiumAccess)
    {
        _premiumAccess = premiumAccess ??
            throw new ArgumentNullException(nameof(premiumAccess));
    }

    public void ProtectRealAI()
    {
        _premiumAccess.EnsureAccess(
            PremiumFeature.RealAITutor);
    }

    public void ProtectAISummarization()
    {
        _premiumAccess.EnsureAccess(
            PremiumFeature.AISummarization);
    }

    public void ProtectAIQuizGeneration()
    {
        _premiumAccess.EnsureAccess(
            PremiumFeature.AIQuizGeneration);
    }

    public void ProtectAdvancedReports()
    {
        _premiumAccess.EnsureAccess(
            PremiumFeature.AdvancedReports);
    }

    public void ProtectCloudSync()
    {
        _premiumAccess.EnsureAccess(
            PremiumFeature.CloudSync);
    }

    public T ExecuteProtected<T>(
        PremiumFeature feature,
        Func<T> operation)
    {
        return _premiumAccess.Execute(
            feature,
            operation);
    }

    public Task<T> ExecuteProtectedAsync<T>(
        PremiumFeature feature,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return _premiumAccess.ExecuteAsync(
            feature,
            operation,
            cancellationToken);
    }
}

