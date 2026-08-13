using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Hayt.Controls;

public partial class ThemeToggleButton : UserControl
{
    private readonly IThemeService _themeService;
    private bool _isSubscribed;

    public ThemeToggleButton()
    {
        InitializeComponent();

        _themeService = ThemeService.Instance;

        Loaded += ThemeToggleButton_Loaded;
        Unloaded += ThemeToggleButton_Unloaded;
    }

    private void ThemeToggleButton_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        SubscribeToThemeChanges();
        UpdateContent();
    }

    private void ThemeToggleButton_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        UnsubscribeFromThemeChanges();
    }

    private void SubscribeToThemeChanges()
    {
        if (_isSubscribed)
        {
            return;
        }

        _themeService.ThemeChanged += ThemeService_ThemeChanged;
        _isSubscribed = true;
    }

    private void UnsubscribeFromThemeChanges()
    {
        if (!_isSubscribed)
        {
            return;
        }

        _themeService.ThemeChanged -= ThemeService_ThemeChanged;
        _isSubscribed = false;
    }

    private void ToggleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _themeService.ToggleTheme();
    }

    private void ThemeService_ThemeChanged(
        object? sender,
        EventArgs e)
    {
        if (Dispatcher.CheckAccess())
        {
            UpdateContent();
            return;
        }

        Dispatcher.Invoke(UpdateContent);
    }

    private void UpdateContent()
    {
        if (_themeService.IsDarkTheme)
        {
            ThemeIcon.Text = "☀";
            ThemeText.Text = "تم روشن";
            ToggleButton.ToolTip = "فعال‌کردن حالت روشن";
        }
        else
        {
            ThemeIcon.Text = "◐";
            ThemeText.Text = "تم تاریک";
            ToggleButton.ToolTip = "فعال‌کردن حالت تاریک";
        }
    }
}

