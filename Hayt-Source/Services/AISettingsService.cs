using System;
using System.IO;
using System.Text.Json;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class AISettingsService : IAISettingsService
{
    private readonly object _sync = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public AIProviderSettings Current { get; private set; } =
        AIProviderSettings.CreateDefault();

    public event EventHandler? SettingsChanged;

    private string AppDirectory
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Hayt");
        }
    }

    private string StateFilePath =>
        Path.Combine(AppDirectory, "ai-settings.json");

    public AIProviderSettings Load()
    {
        lock (_sync)
        {
            try
            {
                if (!Directory.Exists(AppDirectory))
                {
                    Directory.CreateDirectory(AppDirectory);
                }

                if (!File.Exists(StateFilePath))
                {
                    Current = AIProviderSettings.CreateDefault();
                    SaveInternal(Current);
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                    return Current;
                }

                string json = File.ReadAllText(StateFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Current = AIProviderSettings.CreateDefault();
                    SaveInternal(Current);
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                    return Current;
                }

                var settings = JsonSerializer.Deserialize<AIProviderSettings>(json, _jsonOptions)
                    ?? AIProviderSettings.CreateDefault();

                settings.Normalize();

                Current = settings;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return Current;
            }
            catch
            {
                Current = AIProviderSettings.CreateDefault();
                return Current;
            }
        }
    }

    public AIProviderSettings Save(AIProviderSettings settings)
    {
        lock (_sync)
        {
            settings ??= AIProviderSettings.CreateDefault();
            settings.Normalize();
            settings.LastSavedAt = DateTime.Now;

            SaveInternal(settings);

            Current = settings;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            return Current;
        }
    }

    public AIProviderSettings Update(
        bool useRealAI,
        string providerName,
        string baseUrl,
        string model,
        string apiKey,
        int timeoutSeconds,
        double temperature,
        int maxTokens,
        bool allowOfflineFallback)
    {
        var settings = new AIProviderSettings
        {
            UseRealAI = useRealAI,
            ProviderName = providerName,
            BaseUrl = baseUrl,
            Model = model,
            ApiKey = apiKey,
            TimeoutSeconds = timeoutSeconds,
            Temperature = temperature,
            MaxTokens = maxTokens,
            AllowOfflineFallback = allowOfflineFallback,
            LastSavedAt = DateTime.Now
        };

        return Save(settings);
    }

    public AIProviderSettings ResetToDefault()
    {
        return Save(AIProviderSettings.CreateDefault());
    }

    public string GetSettingsFilePath()
    {
        return StateFilePath;
    }

    private void SaveInternal(AIProviderSettings settings)
    {
        if (!Directory.Exists(AppDirectory))
        {
            Directory.CreateDirectory(AppDirectory);
        }

        string json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(StateFilePath, json);
    }
}

