using System;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IAISettingsService
{
    AIProviderSettings Current { get; }

    event EventHandler? SettingsChanged;

    AIProviderSettings Load();

    AIProviderSettings Save(AIProviderSettings settings);

    AIProviderSettings Update(
        bool useRealAI,
        string providerName,
        string baseUrl,
        string model,
        string apiKey,
        int timeoutSeconds,
        double temperature,
        int maxTokens,
        bool allowOfflineFallback);

    AIProviderSettings ResetToDefault();

    string GetSettingsFilePath();
}

