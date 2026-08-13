using System;
using System.Windows;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt
{
    public partial class App
    {
        private void OnExitCleanupSubscription()
        {
            try
            {
                SubscriptionRuntime.Dispose();
            }
            catch
            {
                // پاک‌سازی هنگام خروج نباید برنامه را متوقف کند.
            }
        }
    }
}

