using System;

namespace Hayt.Models;

public sealed class AIProviderSettings
{
    public bool UseRealAI { get; set; }

    public string ProviderName { get; set; } = "OpenAI-Compatible";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

    public string Model { get; set; } = "gpt-4o-mini";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 45;

    public double Temperature { get; set; } = 0.4;

    public int MaxTokens { get; set; } = 1200;

    public bool AllowOfflineFallback { get; set; } = true;

    public DateTime LastSavedAt { get; set; } = DateTime.Now;

    public bool HasApiKey =>
        !string.IsNullOrWhiteSpace(ApiKey);

    public bool CanUseRealAI =>
        UseRealAI &&
        HasApiKey &&
        !string.IsNullOrWhiteSpace(BaseUrl) &&
        !string.IsNullOrWhiteSpace(Model);

    public string ModeText =>
        CanUseRealAI
            ? "مدل واقعی فعال است"
            : "حالت محلی / پشتیبان فعال است";

    public string SafeApiKeyText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                return "تنظیم نشده";
            }

            string key = ApiKey.Trim();

            if (key.Length <= 8)
            {
                return "********";
            }

            return $"{key[..4]}****{key[^4..]}";
        }
    }

    public static AIProviderSettings CreateDefault()
    {
        return new AIProviderSettings
        {
            UseRealAI = false,
            ProviderName = "OpenAI-Compatible",
            BaseUrl = "https://api.openai.com/v1/chat/completions",
            Model = "gpt-4o-mini",
            ApiKey = string.Empty,
            TimeoutSeconds = 45,
            Temperature = 0.4,
            MaxTokens = 1200,
            AllowOfflineFallback = true,
            LastSavedAt = DateTime.Now
        };
    }

    public void Normalize()
    {
        ProviderName = string.IsNullOrWhiteSpace(ProviderName)
            ? "OpenAI-Compatible"
            : ProviderName.Trim();

        BaseUrl = string.IsNullOrWhiteSpace(BaseUrl)
            ? "https://api.openai.com/v1/chat/completions"
            : BaseUrl.Trim();

        Model = string.IsNullOrWhiteSpace(Model)
            ? "gpt-4o-mini"
            : Model.Trim();

        ApiKey = ApiKey?.Trim() ?? string.Empty;

        TimeoutSeconds = Math.Clamp(TimeoutSeconds, 5, 180);
        Temperature = Math.Clamp(Temperature, 0d, 2d);
        MaxTokens = Math.Clamp(MaxTokens, 128, 8000);
    }
}