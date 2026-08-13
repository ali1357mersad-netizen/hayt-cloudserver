using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class AISettingsViewModel : ObservableObject
{
    private readonly IAISettingsService _settingsService;
    private readonly IRealAITutorService _realAIService;

    [ObservableProperty]
    private bool _useRealAI;

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private int _timeoutSeconds = 45;

    [ObservableProperty]
    private double _temperature = 0.4;

    [ObservableProperty]
    private int _maxTokens = 1200;

    [ObservableProperty]
    private bool _allowOfflineFallback = true;

    [ObservableProperty]
    private string _statusMessage = "آماده";

    [ObservableProperty]
    private string _testQuestion = "سلام، خودت را معرفی کن و بگو چگونه به یادگیری من کمک می‌کنی.";

    [ObservableProperty]
    private string _testResult = string.Empty;

    public string ModeText =>
        UseRealAI && !string.IsNullOrWhiteSpace(ApiKey)
            ? "مدل واقعی فعال می‌شود"
            : "حالت محلی / پشتیبان";

    public AISettingsViewModel()
        : this(new AISettingsService())
    {
    }

    public AISettingsViewModel(IAISettingsService settingsService)
        : this(settingsService, new RealAITutorService(settingsService))
    {
    }

    public AISettingsViewModel(
        IAISettingsService settingsService,
        IRealAITutorService realAIService)
    {
        _settingsService = settingsService ??
            throw new ArgumentNullException(nameof(settingsService));

        _realAIService = realAIService ??
            throw new ArgumentNullException(nameof(realAIService));

        LoadFromService();
    }

    [RelayCommand]
    private void Load()
    {
        LoadFromService();
        StatusMessage = "تنظیمات بارگذاری شد.";
    }

    [RelayCommand]
    private void Save()
    {
        var settings = _settingsService.Update(
            UseRealAI,
            ProviderName,
            BaseUrl,
            Model,
            ApiKey,
            TimeoutSeconds,
            Temperature,
            MaxTokens,
            AllowOfflineFallback);

        Apply(settings);
        StatusMessage = "تنظیمات AI ذخیره شد.";
    }

    [RelayCommand]
    private void Reset()
    {
        var settings = _settingsService.ResetToDefault();
        Apply(settings);
        StatusMessage = "تنظیمات به حالت پیش‌فرض برگشت.";
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task TestAsync()
    {
        try
        {
            Save();

            StatusMessage = "در حال تست اتصال به مدل...";
            TestResult = "لطفاً صبر کنید...";

            AIRequestResult result = await _realAIService.AskAsync(
                TestQuestion,
                "این فقط تست اتصال تنظیمات AI در برنامه حیات است.");

            TestResult = result.Content;

            StatusMessage = result.IsSuccess
                ? $"تست موفق - منبع پاسخ: {result.SourceText}"
                : $"تست ناموفق - پاسخ پشتیبان: {result.SourceText} - {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            StatusMessage = "خطا در تست: " + ex.Message;
            TestResult = ex.ToString();
        }
    }

    private void LoadFromService()
    {
        var settings = _settingsService.Load();
        Apply(settings);
    }

    private void Apply(AIProviderSettings settings)
    {
        UseRealAI = settings.UseRealAI;
        ProviderName = settings.ProviderName;
        BaseUrl = settings.BaseUrl;
        Model = settings.Model;
        ApiKey = settings.ApiKey;
        TimeoutSeconds = settings.TimeoutSeconds;
        Temperature = settings.Temperature;
        MaxTokens = settings.MaxTokens;
        AllowOfflineFallback = settings.AllowOfflineFallback;

        OnPropertyChanged(nameof(ModeText));
    }

    partial void OnUseRealAIChanged(bool value)
    {
        OnPropertyChanged(nameof(ModeText));
    }

    partial void OnApiKeyChanged(string value)
    {
        OnPropertyChanged(nameof(ModeText));
    }
}

