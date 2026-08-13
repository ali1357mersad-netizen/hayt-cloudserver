using System;
using System.Threading.Tasks;
using System.Windows;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class AITutorViewModel
{
    private bool _subscriptionEventsConnected;

    private void EnsureSubscriptionEventsConnected()
    {
        if (_subscriptionEventsConnected)
        {
            return;
        }

        _subscriptionEventsConnected = true;

        // Start و LicenseRefreshed اعضای ایستای Runtime هستند.
        SubscriptionRuntime.Start();

        SubscriptionRuntime.LicenseRefreshed +=
            OnGlobalLicenseRefreshed;
    }

    private void OnGlobalLicenseRefreshed(
        object? sender,
        SubscriptionSnapshot snapshot)
    {
        void ApplySnapshot()
        {
            if (snapshot.HasPremiumAccess)
            {
                HidePremiumUpgrade();
                return;
            }

            ShowPremiumUpgrade(
                snapshot.Message ??
                "برای استفاده از هوش مصنوعی واقعی، " +
                "لایسنس Premium را فعال کنید.");
        }

        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is not null &&
            !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke((Action)ApplySnapshot);
            return;
        }

        ApplySnapshot();
    }

    private async Task RefreshLicenseAfterActivationAsync()
    {
        EnsureSubscriptionEventsConnected();

        try
        {
            // snapshot باید از نتیجه Refresh دریافت شود.
            var snapshot = await SubscriptionRuntime.RefreshAsync(
                forceRefresh: true);

            // Runtime رویداد را منتشر کرده است؛ اجرای مستقیم نیز
            // هماهنگی فوری همین ViewModel را تضمین می‌کند.
            OnGlobalLicenseRefreshed(
                SubscriptionRuntime.Current,
                snapshot);
        }
        catch
        {
            // خطا نباید fallback آفلاین را متوقف کند.
            // هیچ دسترسی جدیدی نیز هنگام خطا اعطا نمی‌شود.
        }
    }
}

