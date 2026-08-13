using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Licensing.Services;

/// <summary>
/// Refresh دوره‌ای اشتراک در زمان اجرای برنامه.
/// این سرویس هیچ Thread دائمی ایجاد نمی‌کند و قابل Dispose است.
/// </summary>
public sealed class SubscriptionRefreshService :
    IAsyncDisposable,
    IDisposable
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _stopSource = new();

    private Task? _worker;
    private bool _disposed;

    public SubscriptionRefreshService(
        ISubscriptionService subscriptionService,
        TimeSpan? interval = null)
    {
        _subscriptionService = subscriptionService ??
            throw new ArgumentNullException(
                nameof(subscriptionService));

        _interval = interval ?? TimeSpan.FromMinutes(15);

        if (_interval < TimeSpan.FromMinutes(1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                "فاصله Refresh نباید کمتر از یک دقیقه باشد.");
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_worker is not null)
        {
            return;
        }

        _worker = RunAsync(_stopSource.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.RefreshAsync(
                forceRefresh: true,
                cancellationToken);

            using var timer = new PeriodicTimer(_interval);

            while (
                await timer.WaitForNextTickAsync(cancellationToken)
            )
            {
                try
                {
                    await _subscriptionService.RefreshAsync(
                        forceRefresh: true,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    // Fail closed؛ خطای شبکه یا فایل نباید برنامه را ببندد.
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopSource.Cancel();
        _stopSource.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopSource.Cancel();

        if (_worker is not null)
        {
            try
            {
                await _worker;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _stopSource.Dispose();
    }
}

