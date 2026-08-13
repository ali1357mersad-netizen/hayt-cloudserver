using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Hayt.Services;

public sealed class PremiumUpgradeUIService
{
    private static readonly string[] PreferredWindowNames = { "LicenseActivationWindow", "ActivateLicenseWindow", "LicenseWindow", "SubscriptionWindow", "PremiumWindow", "SettingsWindow" };

    public bool OpenLicenseActivation()
    {
        try
        {
            var windowType = FindPreferredWindowType();
            if (windowType is null) { ShowFallbackMessage(); return false; }
            if (Activator.CreateInstance(windowType) is not Window window) { ShowFallbackMessage(); return false; }
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive);
            if (owner is not null && !ReferenceEquals(owner, window)) { window.Owner = owner; window.WindowStartupLocation = WindowStartupLocation.CenterOwner; }
            else { window.WindowStartupLocation = WindowStartupLocation.CenterScreen; }
            window.ShowDialog();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("باز کردن پنجره فعال‌سازی لایسنس ممکن نشد.\n\n" + ex.Message, "فعال‌سازی لایسنس", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private static Type? FindPreferredWindowType()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Type[] types;
        try { types = assembly.GetTypes(); } catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(type => type is not null).Cast<Type>().ToArray(); }
        var windowTypes = types.Where(type => !type.IsAbstract && typeof(Window).IsAssignableFrom(type)).ToArray();
        foreach (var preferredName in PreferredWindowNames)
        {
            var exactMatch = windowTypes.FirstOrDefault(type => string.Equals(type.Name, preferredName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch is not null) return exactMatch;
        }
        return windowTypes.FirstOrDefault(type => type.Name.Contains("License", StringComparison.OrdinalIgnoreCase) || type.Name.Contains("Activation", StringComparison.OrdinalIgnoreCase) || type.Name.Contains("Subscription", StringComparison.OrdinalIgnoreCase) || type.Name.Contains("Premium", StringComparison.OrdinalIgnoreCase));
    }

    private static void ShowFallbackMessage()
    {
        MessageBox.Show("برای فعال‌سازی قابلیت‌های Premium، از بخش تنظیمات یا مدیریت لایسنس، فایل لایسنس معتبر خود را انتخاب کنید.", "فعال‌سازی قابلیت‌های Premium", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}