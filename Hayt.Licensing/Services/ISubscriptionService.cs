using Hayt.Licensing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

public interface ISubscriptionService
{
    event EventHandler<SubscriptionSnapshot>? SubscriptionChanged;

    SubscriptionSnapshot Current { get; }

    Task<SubscriptionSnapshot> RefreshAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    void InvalidateCache();

    bool HasPremiumAccess();
}

