using Hayt.Licensing.Services;
using System;
using System.Threading.Tasks;
using Hayt.Licensing.Models;

namespace Hayt.Services
{
    /// <summary>
    /// سرویس سراسری زمان اجرا برای مدیریت لایسنس اشتراک.
    /// </summary>
    public static class SubscriptionRuntime
    {
        private static SubscriptionService? _service;
        private static readonly object _lock = new();
        private static bool _isRefreshing;

        /// <summary>
        /// رویداد سراسری پس از هر Refresh موفق منتشر می‌شود.
        /// </summary>
        public static event EventHandler<SubscriptionSnapshot>? LicenseRefreshed;

        public static SubscriptionService Current
        {
            get
            {
                lock (_lock)
                {
                    if (_service is null)
                    {
                        _service = new SubscriptionService();
                    }
                    return _service;
                }
            }
        }

        public static SubscriptionService SubscriptionService => Current;

        public static void Start()
        {
            // اطمینان از ساخت سرویس
            _ = Current;
        }

        public static async Task<SubscriptionSnapshot> RefreshAsync(
            bool forceRefresh = false)
        {
            if (_isRefreshing)
            {
                return Current.Current;
            }

            _isRefreshing = true;
            try
            {
                var snapshot = await Current.RefreshAsync(forceRefresh);
                LicenseRefreshed?.Invoke(null, snapshot);
                return snapshot;
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public static void Dispose()
        {
            lock (_lock)
            {
                _service = null;
            }
        }
    }
}

