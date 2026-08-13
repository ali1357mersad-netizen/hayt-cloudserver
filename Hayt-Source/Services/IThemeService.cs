using System;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    bool IsDarkTheme { get; }

    event EventHandler? ThemeChanged;

    void LoadAndApply();

    void ApplyTheme(AppTheme theme);

    void ToggleTheme();
}

