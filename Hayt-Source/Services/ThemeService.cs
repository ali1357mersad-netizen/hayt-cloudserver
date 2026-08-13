using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class ThemeService : IThemeService
{
    private static readonly Lazy<ThemeService> LazyInstance =
        new(() => new ThemeService());

    private readonly string _settingsDirectory;
    private readonly string _settingsFile;

    public static ThemeService Instance => LazyInstance.Value;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public bool IsDarkTheme => CurrentTheme == AppTheme.Dark;

    public event EventHandler? ThemeChanged;

    private ThemeService()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Hayt");

        _settingsFile = Path.Combine(
            _settingsDirectory,
            "theme-settings.json");
    }

    public void LoadAndApply()
    {
        AppTheme theme = AppTheme.Light;

        try
        {
            if (File.Exists(_settingsFile))
            {
                string json = File.ReadAllText(_settingsFile);

                ThemeSettings? settings =
                    JsonSerializer.Deserialize<ThemeSettings>(json);

                if (settings is not null &&
                    Enum.TryParse(
                        settings.Theme,
                        ignoreCase: true,
                        out AppTheme savedTheme))
                {
                    theme = savedTheme;
                }
            }
        }
        catch
        {
            // خرابی فایل تنظیمات نباید مانع اجرای برنامه شود.
            theme = AppTheme.Light;
        }

        ApplyTheme(theme, save: false);
    }

    public void ApplyTheme(AppTheme theme)
    {
        ApplyTheme(theme, save: true);
    }

    public void ToggleTheme()
    {
        ApplyTheme(
            IsDarkTheme
                ? AppTheme.Light
                : AppTheme.Dark);
    }

    private void ApplyTheme(AppTheme theme, bool save)
    {
        CurrentTheme = theme;

        IReadOnlyDictionary<string, string> palette =
            theme == AppTheme.Dark
                ? CreateDarkPalette()
                : CreateLightPalette();

        if (Application.Current is not null)
        {
            foreach (KeyValuePair<string, string> item in palette)
            {
                SetBrushColor(
                    Application.Current.Resources,
                    item.Key,
                    item.Value);
            }
        }

        if (save)
        {
            Save();
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);

            var settings = new ThemeSettings
            {
                Theme = CurrentTheme.ToString()
            };

            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_settingsFile, json);
        }
        catch
        {
            // خطای ذخیره تنظیمات نباید برنامه را متوقف کند.
        }
    }

    private static void SetBrushColor(
        ResourceDictionary dictionary,
        string key,
        string colorCode)
    {
        if (dictionary.Contains(key))
        {
            Color color =
                (Color)ColorConverter.ConvertFromString(colorCode);

            if (dictionary[key] is SolidColorBrush brush)
            {
                if (brush.IsFrozen)
                {
                    dictionary[key] = new SolidColorBrush(color);
                }
                else
                {
                    brush.Color = color;
                }
            }
            else
            {
                dictionary[key] = new SolidColorBrush(color);
            }

            return;
        }

        foreach (ResourceDictionary mergedDictionary
                 in dictionary.MergedDictionaries)
        {
            SetBrushColor(mergedDictionary, key, colorCode);
        }
    }

    private static IReadOnlyDictionary<string, string>
        CreateLightPalette()
    {
        return new Dictionary<string, string>
        {
            ["HaytPrimaryBrush"] = "#0F3D3E",
            ["HaytPrimaryLightBrush"] = "#147C78",
            ["HaytAccentBrush"] = "#1DA79F",
            ["HaytBackgroundBrush"] = "#F4F7F8",
            ["HaytSurfaceBrush"] = "#FFFFFF",
            ["HaytBorderBrush"] = "#E1E8EA",
            ["HaytTextBrush"] = "#12393B",
            ["HaytMutedTextBrush"] = "#607D80",
            ["HaytDangerBrush"] = "#C0392B",
            ["HaytSuccessBrush"] = "#147C78"
        };
    }

    private static IReadOnlyDictionary<string, string>
        CreateDarkPalette()
    {
        return new Dictionary<string, string>
        {
            ["HaytPrimaryBrush"] = "#071F20",
            ["HaytPrimaryLightBrush"] = "#168B85",
            ["HaytAccentBrush"] = "#2AC7BD",
            ["HaytBackgroundBrush"] = "#101718",
            ["HaytSurfaceBrush"] = "#192324",
            ["HaytBorderBrush"] = "#334445",
            ["HaytTextBrush"] = "#EDF7F6",
            ["HaytMutedTextBrush"] = "#A7BCBA",
            ["HaytDangerBrush"] = "#E56B60",
            ["HaytSuccessBrush"] = "#35BFAF"
        };
    }

    private sealed class ThemeSettings
    {
        public string Theme { get; set; } = AppTheme.Light.ToString();
    }
}

