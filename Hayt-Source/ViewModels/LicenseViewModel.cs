using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class LicenseViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;

    [ObservableProperty]
    private LicenseState _state = LicenseState.CreateDefault();

    [ObservableProperty]
    private string _combinedLicenseText = string.Empty;

    [ObservableProperty]
    private string _payloadBase64 = string.Empty;

    [ObservableProperty]
    private string _signatureBase64 = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "آماده";

    [ObservableProperty]
    private string _machineId = string.Empty;

    [ObservableProperty]
    private string _unsignedPayloadPreview = string.Empty;

    [ObservableProperty]
    private string _generatorUserName = string.Empty;

    [ObservableProperty]
    private string _generatorUserEmail = string.Empty;

    [ObservableProperty]
    private LicensePlan _generatorPlan = LicensePlan.Premium;

    [ObservableProperty]
    private bool _generatorBindToThisMachine = true;

    [ObservableProperty]
    private int _generatorValidDays = 365;

    public string WindowTitleText =>
        "لایسنس و فعال‌سازی - " + State.StatusText;

    public string StatusText =>
        State.StatusText;

    public string PlanText =>
        State.EffectivePlan switch
        {
            LicensePlan.Free => "رایگان",
            LicensePlan.Trial => "آزمایشی",
            LicensePlan.Premium => "Premium",
            LicensePlan.Lifetime => "دائمی",
            LicensePlan.Enterprise => "سازمانی",
            _ => "نامشخص"
        };

    public bool HasPremiumAccess =>
        State.HasPremiumAccess;

    public int TrialDaysLeft =>
        State.TrialDaysLeft;

    public string LicenseUserText =>
        State.Payload is null
            ? "فعال نشده"
            : State.Payload.DisplayUser;

    public string ExpiryText =>
        State.Payload is null
            ? "نامشخص"
            : State.Payload.ExpiryText;

    public LicenseViewModel()
        : this(new LicenseService())
    {
    }

    public LicenseViewModel(ILicenseService licenseService)
    {
        _licenseService = licenseService ??
            throw new ArgumentNullException(nameof(licenseService));

        _licenseService.LicenseChanged += (_, _) =>
        {
            State = _licenseService.Current;
            RaiseState();
        };

        Load();
    }

    [RelayCommand]
    private void Load()
    {
        State = _licenseService.Load();
        MachineId = _licenseService.GetMachineId();
        StatusMessage = "وضعیت لایسنس بارگذاری شد.";
        RaiseState();
    }

    [RelayCommand]
    private void Validate()
    {
        LicenseValidationResult result = _licenseService.ValidateCurrent();

        State = _licenseService.Current;
        StatusMessage = result.Message;

        RaiseState();
    }

    [RelayCommand]
    private void ActivateCombined()
    {
        LicenseValidationResult result = _licenseService.ActivateFromCombinedText(CombinedLicenseText);

        State = _licenseService.Current;
        StatusMessage = result.Message;

        RaiseState();
    }

    [RelayCommand]
    private void ActivateParts()
    {
        LicenseValidationResult result = _licenseService.Activate(PayloadBase64, SignatureBase64);

        State = _licenseService.Current;
        StatusMessage = result.Message;

        RaiseState();
    }

    [RelayCommand]
    private void Deactivate()
    {
        _licenseService.Deactivate();

        State = _licenseService.Current;
        StatusMessage = "لایسنس غیرفعال شد.";

        RaiseState();
    }

    [RelayCommand]
    private void GenerateUnsignedPayloadPreview()
    {
        DateTime? expiresAt = null;

        if (GeneratorPlan != LicensePlan.Lifetime)
        {
            expiresAt = DateTime.UtcNow.AddDays(Math.Max(1, GeneratorValidDays));
        }

        UnsignedPayloadPreview = _licenseService.CreateUnsignedLicensePayloadJson(
            GeneratorUserName,
            GeneratorUserEmail,
            GeneratorPlan,
            expiresAt,
            GeneratorBindToThisMachine);

        StatusMessage = "Payload خام ساخته شد. در مرحله 17-A2 ابزار امضای آن اضافه می‌شود.";
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(WindowTitleText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PlanText));
        OnPropertyChanged(nameof(HasPremiumAccess));
        OnPropertyChanged(nameof(TrialDaysLeft));
        OnPropertyChanged(nameof(LicenseUserText));
        OnPropertyChanged(nameof(ExpiryText));
    }

    partial void OnStateChanged(LicenseState value)
    {
        RaiseState();
    }
}

