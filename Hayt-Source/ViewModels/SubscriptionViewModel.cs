using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public sealed class SubscriptionViewModel :
    INotifyPropertyChanged
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly PremiumUpgradeUIService _upgradeUIService;

    private SubscriptionSnapshot _snapshot =
        SubscriptionSnapshot.Free();

    private bool _isBusy;
    private string? _errorMessage;

    public SubscriptionViewModel()
        : this(
            new SubscriptionService(),
            new PremiumUpgradeUIService())
    {
    }

    public SubscriptionViewModel(
        ISubscriptionService subscriptionService,
        PremiumUpgradeUIService upgradeUIService)
    {
        _subscriptionService = subscriptionService ??
            throw new ArgumentNullException(
                nameof(subscriptionService));

        _upgradeUIService = upgradeUIService ??
            throw new ArgumentNullException(
                nameof(upgradeUIService));

        RefreshCommand = new AsyncCommand(
            RefreshAsync,
            () => !IsBusy);

        ActivateLicenseCommand = new RelayActionCommand(
            ActivateLicense,
            () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand RefreshCommand { get; }

    public ICommand ActivateLicenseCommand { get; }

    public SubscriptionSnapshot Snapshot
    {
        get => _snapshot;
        private set
        {
            if (Equals(_snapshot, value))
            {
                return;
            }

            _snapshot = value;
            RaiseAllSubscriptionProperties();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public string StatusText => Snapshot.Status switch
    {
        SubscriptionStatus.Free => "رایگان",
        SubscriptionStatus.Trial => "آزمایشی",
        SubscriptionStatus.Active => "فعال",
        SubscriptionStatus.GracePeriod => "مهلت ارفاقی",
        SubscriptionStatus.Expired => "منقضی‌شده",
        SubscriptionStatus.Lifetime => "دائمی",
        SubscriptionStatus.Enterprise => "سازمانی",
        _ => "نامعتبر"
    };

    public string PlanText => Snapshot.PlanName;

    public string AccessText =>
        Snapshot.HasPremiumAccess
            ? "دسترسی Premium فعال است"
            : "دسترسی Premium غیرفعال است";

    public string StartDateText =>
        FormatDate(Snapshot.StartedAtUtc);

    public string ExpiryDateText =>
        Snapshot.IsLifetime
            ? "بدون انقضا"
            : FormatDate(Snapshot.ExpiresAtUtc);

    public string GraceDateText =>
        FormatDate(Snapshot.GraceEndsAtUtc);

    public string RemainingText =>
        Snapshot.IsLifetime
            ? "دائمی"
            : Snapshot.RemainingDays is int days
                ? $"{days} روز"
                : "نامشخص";

    public string CheckedAtText =>
        Snapshot.CheckedAtUtc
            .ToLocalTime()
            .ToString("yyyy/MM/dd HH:mm");

    public string LicenseIdText =>
        string.IsNullOrWhiteSpace(Snapshot.LicenseId)
            ? "ثبت نشده"
            : Snapshot.LicenseId;

    public string MachineIdText =>
        string.IsNullOrWhiteSpace(Snapshot.MachineId)
            ? "اتصال به دستگاه ثبت نشده"
            : Snapshot.MachineId;

    public string MessageText =>
        Snapshot.Message ?? string.Empty;

    public bool CanActivate =>
        !Snapshot.HasPremiumAccess;

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            _subscriptionService.InvalidateCache();

            Snapshot = await _subscriptionService.RefreshAsync(
                forceRefresh: true);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                "بررسی وضعیت اشتراک ممکن نشد: " +
                ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ActivateLicense()
    {
        try
        {
            _upgradeUIService.OpenLicenseActivation();

            // بعد از بسته‌شدن پنجره فعال‌سازی، وضعیت دوباره خوانده می‌شود.
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                "فعال‌سازی لایسنس ممکن نشد: " +
                ex.Message;
        }
    }

    private void RaiseAllSubscriptionProperties()
    {
        OnPropertyChanged(nameof(Snapshot));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PlanText));
        OnPropertyChanged(nameof(AccessText));
        OnPropertyChanged(nameof(StartDateText));
        OnPropertyChanged(nameof(ExpiryDateText));
        OnPropertyChanged(nameof(GraceDateText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(CheckedAtText));
        OnPropertyChanged(nameof(LicenseIdText));
        OnPropertyChanged(nameof(MachineIdText));
        OnPropertyChanged(nameof(MessageText));
        OnPropertyChanged(nameof(CanActivate));
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        return value is null
            ? "نامشخص"
            : value.Value
                .ToLocalTime()
                .ToString("yyyy/MM/dd HH:mm");
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayActionCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayActionCommand(
            Action execute,
            Func<bool>? canExecute = null)
        {
            _execute = execute ??
                throw new ArgumentNullException(nameof(execute));

            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) =>
            _execute();
    }

    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncCommand(
            Func<Task> execute,
            Func<bool>? canExecute = null)
        {
            _execute = execute ??
                throw new ArgumentNullException(nameof(execute));

            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) =>
            !_isExecuting &&
            (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}

