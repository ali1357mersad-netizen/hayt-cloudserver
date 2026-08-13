using System;
using System.Threading.Tasks;
using System.Windows;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.ViewModels;
using Hayt.Views;

namespace Hayt;

public partial class MainWindow
{
    private bool _isSubscriptionWindowOpen;

    private async void OpenSubscriptionMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isSubscriptionWindowOpen)
        {
            return;
        }

        _isSubscriptionWindowOpen = true;

        try
        {
            // Start متعلق به Runtime است، نه SubscriptionService.
            SubscriptionRuntime.Start();

            // Current خودش نمونه SubscriptionService است.
            var viewModel = new SubscriptionViewModel(
                SubscriptionRuntime.Current,
                new PremiumUpgradeUIService());

            var subscriptionWindow =
                new SubscriptionWindow(viewModel)
                {
                    Owner = this,
                    WindowStartupLocation =
                        WindowStartupLocation.CenterOwner
                };

            subscriptionWindow.ShowDialog();

            // اعمال تغییر احتمالی لایسنس پس از بسته‌شدن پنجره.
            await RefreshSubscriptionAfterWindowAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "باز کردن بخش Premium و اشتراک ممکن نشد.\n\n" +
                ex.Message,
                "Premium و اشتراک",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _isSubscriptionWindowOpen = false;
        }
    }

    private static async Task RefreshSubscriptionAfterWindowAsync()
    {
        try
        {
            // Refresh و انتشار رویداد باید از Runtime انجام شود.
            await SubscriptionRuntime.RefreshAsync(
                forceRefresh: true);
        }
        catch
        {
            // خطای Refresh نباید برنامه را متوقف کند.
            // License Gate همچنان Fail-Closed باقی می‌ماند.
        }
    }
}

