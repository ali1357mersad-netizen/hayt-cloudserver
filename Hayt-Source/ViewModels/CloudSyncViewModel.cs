using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Hayt.Services.CloudSync;

namespace Hayt.ViewModels
{
    public sealed class CloudSyncViewModel : BaseViewModel
    {
        private readonly ICloudSyncService _syncService;
        private readonly Func<bool> _hasPremiumAccess;
        private readonly Func<bool> _isOnline;

        private CloudSyncState _state;
        private string _statusText;
        private string _lastSyncText;
        private int _pendingCount;
        private bool _isSynchronizing;
        private bool _isAutoSyncEnabled;
        private bool _isPremiumUser;

        public CloudSyncViewModel(
            ICloudSyncService syncService,
            Func<bool> hasPremiumAccess,
            Func<bool> isOnline)
        {
            _syncService = syncService;
            _hasPremiumAccess = hasPremiumAccess;
            _isOnline = isOnline;

            _state = CloudSyncState.Offline;
            _statusText = "آماده بررسی وضعیت";
            _lastSyncText = "هنوز همگام‌سازی انجام نشده";
            _pendingCount = 0;
            _isSynchronizing = false;
            _isAutoSyncEnabled = true;
            _isPremiumUser = hasPremiumAccess();

            PendingItems = new ObservableCollection<CloudSyncQueueItem>();

            SynchronizeCommand = new RelayCommand(
                async () => await SynchronizeAsync(),
                () => !IsSynchronizing);

            RefreshCommand = new RelayCommand(
                async () => await RefreshAsync());

            ToggleAutoSyncCommand = new RelayCommand(
                () => IsAutoSyncEnabled = !IsAutoSyncEnabled);
        }

        public ObservableCollection<CloudSyncQueueItem> PendingItems { get; }

        public ICommand SynchronizeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleAutoSyncCommand { get; }

        public CloudSyncState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StateText));
                    OnPropertyChanged(nameof(StateEmoji));
                    OnPropertyChanged(nameof(StateColor));
                }
            }
        }

        public string StateText => State switch
        {
            CloudSyncState.Offline => "آفلاین",
            CloudSyncState.Ready => "آماده",
            CloudSyncState.Synchronizing => "در حال همگام‌سازی",
            CloudSyncState.PremiumRequired => "نیازمند Premium",
            CloudSyncState.Failed => "خطا",
            _ => "نامشخص"
        };

        public string StateEmoji => State switch
        {
            CloudSyncState.Offline => "📴",
            CloudSyncState.Ready => "✅",
            CloudSyncState.Synchronizing => "🔄",
            CloudSyncState.PremiumRequired => "💎",
            CloudSyncState.Failed => "❌",
            _ => "❓"
        };

        public string StateColor => State switch
        {
            CloudSyncState.Offline => "#8A8A8A",
            CloudSyncState.Ready => "#176B52",
            CloudSyncState.Synchronizing => "#D6A84B",
            CloudSyncState.PremiumRequired => "#7B5EA7",
            CloudSyncState.Failed => "#C0392B",
            _ => "#8A8A8A"
        };

        public string StatusText
        {
            get => _statusText;
            private set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastSyncText
        {
            get => _lastSyncText;
            private set
            {
                if (_lastSyncText != value)
                {
                    _lastSyncText = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PendingCount
        {
            get => _pendingCount;
            private set
            {
                if (_pendingCount != value)
                {
                    _pendingCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PendingCountText));
                }
            }
        }

        public string PendingCountText =>
            $"آیتم‌های در انتظار: {PendingCount}";

        public bool IsSynchronizing
        {
            get => _isSynchronizing;
            private set
            {
                if (_isSynchronizing != value)
                {
                    _isSynchronizing = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNotSynchronizing));
                    ((RelayCommand)SynchronizeCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsNotSynchronizing => !IsSynchronizing;

        public bool IsAutoSyncEnabled
        {
            get => _isAutoSyncEnabled;
            set
            {
                if (_isAutoSyncEnabled != value)
                {
                    _isAutoSyncEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPremiumUser
        {
            get => _isPremiumUser;
            private set
            {
                if (_isPremiumUser != value)
                {
                    _isPremiumUser = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PremiumStatusText));
                }
            }
        }

        public string PremiumStatusText =>
            IsPremiumUser
                ? "💎 کاربر Premium"
                : "🔓 کاربر رایگان — Cloud Sync نیازمند Premium است";

        public async Task InitializeAsync()
        {
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            try
            {
                var pending = await _syncService.GetPendingAsync();
                PendingItems.Clear();
                foreach (var item in pending)
                {
                    PendingItems.Add(item);
                }
                PendingCount = pending.Count;

                State = _syncService.State;
                IsPremiumUser = _hasPremiumAccess();

                if (!IsPremiumUser)
                {
                    StatusText = "Cloud Sync فقط برای کاربران Premium فعال است.";
                }
                else if (!_isOnline())
                {
                    StatusText = "اینترنت در دسترس نیست؛ اطلاعات در صف امن باقی مانده است.";
                }
                else
                {
                    StatusText = "زیرساخت Cloud Sync آماده است.";
                }
            }
            catch (Exception ex)
            {
                State = CloudSyncState.Failed;
                StatusText = $"خطا در بررسی وضعیت: {ex.Message}";
            }
        }

        public async Task SynchronizeAsync()
        {
            if (IsSynchronizing)
            {
                return;
            }

            IsSynchronizing = true;
            State = CloudSyncState.Synchronizing;
            StatusText = "در حال همگام‌سازی...";

            try
            {
                var result = await _syncService.SynchronizeAsync();
                State = result.State;
                StatusText = result.Message;
                PendingCount = result.PendingItems;
                LastSyncText = $"آخرین همگام‌سازی: {DateTime.Now:yyyy/MM/dd HH:mm}";

                await RefreshAsync();
            }
            catch (Exception ex)
            {
                State = CloudSyncState.Failed;
                StatusText = $"خطا در همگام‌سازی: {ex.Message}";
            }
            finally
            {
                IsSynchronizing = false;
            }
        }
    }
}

