using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.Services.CloudSync;

namespace Hayt.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    [ObservableProperty]
    private DashboardSnapshot? _snapshot;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime? _lastRefreshAt;

    [ObservableProperty]
    private string _statusText = "آماده";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _selectedFilterOption = "۷ روز اخیر";

    public NotificationViewModel NotificationVM { get; }
    public AchievementViewModel AchievementVM { get; }
    public StreakViewModel StreakVM { get; }
    public PersonalGoalsViewModel GoalsVM { get; }
    public CloudSyncViewModel CloudSyncVM { get; }

    public ObservableCollection<string> FilterOptions { get; } = new()
    {
        "امروز",
        "۷ روز اخیر",
        "۳۰ روز اخیر",
        "همه"
    };

    public ObservableCollection<DashboardChartPoint> WeeklyActivity { get; } = new();
    public ObservableCollection<DashboardChartPoint> ProgressDistribution { get; } = new();
    public ObservableCollection<DashboardChartPoint> QuizPerformance { get; } = new();
    public ObservableCollection<DashboardBookProgressItem> TopBooks { get; } = new();
    public ObservableCollection<DashboardBookProgressItem> RecentBooks { get; } = new();
    public ObservableCollection<DashboardRecentActivityItem> RecentActivities { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSnapshot => Snapshot is not null;
    public bool HasAnyBooks => TopBooks.Any() || RecentBooks.Any();
    public bool HasNoData => !IsLoading && !HasError && !HasAnyBooks && WeeklyActivity.Count == 0 && RecentActivities.Count == 0;

    public bool HasWeeklyActivitySection => WeeklyActivity.Count > 0;
    public bool HasTopBooksSection => TopBooks.Count > 0;
    public bool HasRecentBooksSection => RecentBooks.Count > 0;
    public bool HasRecentActivitiesSection => RecentActivities.Count > 0;

    public string DashboardStatusText =>
        LastRefreshAt is null
            ? "هنوز بروزرسانی نشده"
            : $"آخرین بروزرسانی: {LastRefreshAt:yyyy/MM/dd HH:mm}";

    public string FilterDescription =>
        SelectedFilterOption switch
        {
            "امروز" => "نمایش اطلاعات مربوط به امروز",
            "۷ روز اخیر" => "نمایش اطلاعات ۷ روز اخیر",
            "۳۰ روز اخیر" => "نمایش اطلاعات ۳۰ روز اخیر",
            _ => "نمایش تمام اطلاعات موجود"
        };

    public int FilteredBookCount => DistinctFilteredBooks().Count();
    public int FilteredRecentCount => RecentBooks.Count;
    public int FilteredWeeklyPointCount => WeeklyActivity.Count;
    public int FilteredActivityCount => RecentActivities.Count;
    public double FilteredAverageProgressPercent =>
        DistinctFilteredBooks().Any()
            ? DistinctFilteredBooks().Average(x => x.ProgressPercent)
            : 0;

    public int FilteredActivityValueTotal => WeeklyActivity.Sum(x => SafeInt(x.Value));

    public DashboardViewModel(
        IDashboardService dashboardService,
        NotificationViewModel notificationVM,
        AchievementViewModel? achievementVM = null,
        StreakViewModel? streakVM = null,
        PersonalGoalsViewModel? goalsVM = null,
        CloudSyncViewModel? cloudSyncVM = null)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        NotificationVM = notificationVM ?? throw new ArgumentNullException(nameof(notificationVM));
        AchievementVM = achievementVM ?? throw new ArgumentNullException(nameof(achievementVM), "AchievementViewModel تزریق نشده است.");
        StreakVM = streakVM ?? throw new ArgumentNullException(nameof(streakVM), "StreakViewModel تزریق نشده است.");
        GoalsVM = goalsVM ?? new PersonalGoalsViewModel(new PersonalGoalsService());
        CloudSyncVM = cloudSyncVM ?? CreateDefaultCloudSyncVM();
    }

    private static CloudSyncViewModel CreateDefaultCloudSyncVM()
    {
        string appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        var service = CloudSyncServiceFactory.Create(
            appData,
            hasPremiumAccess: () => false,
            isOnline: () => true);

        return new CloudSyncViewModel(
            service,
            hasPremiumAccess: () => false,
            isOnline: () => true);
    }

    private bool CanRunDashboardOperation() => !IsLoading;

    partial void OnIsLoadingChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        RaiseSectionProperties();
        OnPropertyChanged(nameof(HasNoData));
    }

    partial void OnSnapshotChanged(DashboardSnapshot? value)
    {
        OnPropertyChanged(nameof(HasSnapshot));
        RebuildFilteredCollections();
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasNoData));
    }

    partial void OnSelectedFilterOptionChanged(string value)
    {
        RebuildFilteredCollections();
        StatusText = "آماده";
    }

    [RelayCommand(CanExecute = nameof(CanRunDashboardOperation))]
    public Task LoadAsync() => ExecuteDashboardOperationAsync(isRefresh: false);

    [RelayCommand(CanExecute = nameof(CanRunDashboardOperation))]
    public Task RefreshAsync() => ExecuteDashboardOperationAsync(isRefresh: true);

    [RelayCommand]
    private void DismissError()
    {
        ErrorMessage = null;

        if (!IsLoading)
            StatusText = "آماده";
    }

    private async Task ExecuteDashboardOperationAsync(bool isRefresh)
    {
        var lockTaken = await _refreshSemaphore.WaitAsync(0);
        if (!lockTaken)
            return;

        try
        {
            IsLoading = true;
            StatusText = "در حال بروزرسانی...";
            ErrorMessage = null;

            var newSnapshot = await _dashboardService.GetSnapshotAsync();

            Snapshot = newSnapshot;
            LastRefreshAt = DateTime.Now;

            RebuildFilteredCollections();
            await AchievementVM.EvaluateAsync(newSnapshot);
            StreakVM.Evaluate(newSnapshot);
            GoalsVM.Evaluate(newSnapshot);

            StatusText = "آماده";
            OnPropertyChanged(nameof(DashboardStatusText));

            if (isRefresh)
            {
                NotificationVM.AddTestInfoCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            StatusText = "بروزرسانی ناموفق بود";
            ErrorMessage = $"دریافت اطلاعات داشبورد انجام نشد. لطفاً دوباره تلاش کنید. جزئیات: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            _refreshSemaphore.Release();
        }
    }

    private void RebuildFilteredCollections()
    {
        var snapshot = Snapshot;

        if (snapshot is null)
        {
            WeeklyActivity.Clear();
            ProgressDistribution.Clear();
            QuizPerformance.Clear();
            TopBooks.Clear();
            RecentBooks.Clear();
            RecentActivities.Clear();
            RaiseFilterDependentProperties();
            RaiseSectionProperties();
            return;
        }

        var dateRange = GetSelectedDateRange();

        var filteredWeeklyRaw = ApplyDateFilter(
                snapshot.WeeklyActivity ?? Enumerable.Empty<DashboardChartPoint>(),
                x => x.Date,
                dateRange)
            .OrderBy(x => x.Date)
            .ToList();

        var normalizedWeekly = NormalizeWeeklyActivity(filteredWeeklyRaw);

        var allBooks = snapshot.Books ?? Enumerable.Empty<DashboardBookProgressItem>();

        var filteredBooks = ApplyDateFilter(
                allBooks,
                x => x.LastActivityAt,
                dateRange)
            .ToList();

        var topBooks = filteredBooks
            .OrderByDescending(x => x.ProgressPercent)
            .ThenByDescending(x => x.EarnedXp)
            .Take(5)
            .ToList();

        var recentBooks = filteredBooks
            .OrderByDescending(x => x.LastActivityAt)
            .ThenByDescending(x => x.ProgressPercent)
            .Take(5)
            .ToList();

        var recentActivities = BuildRecentActivities(filteredBooks, filteredWeeklyRaw);

        ReplaceCollection(WeeklyActivity, normalizedWeekly);
        ReplaceCollection(ProgressDistribution, snapshot.ProgressDistribution ?? Enumerable.Empty<DashboardChartPoint>());
        ReplaceCollection(QuizPerformance, snapshot.QuizPerformance ?? Enumerable.Empty<DashboardChartPoint>());
        ReplaceCollection(TopBooks, topBooks);
        ReplaceCollection(RecentBooks, recentBooks);
        ReplaceCollection(RecentActivities, recentActivities);

        RaiseFilterDependentProperties();
        RaiseSectionProperties();
    }

    private static List<DashboardChartPoint> NormalizeWeeklyActivity(IEnumerable<DashboardChartPoint> source)
    {
        var items = source.ToList();

        if (items.Count == 0)
            return new List<DashboardChartPoint>();

        var max = items.Max(x => Math.Max(0, SafeInt(x.Value)));

        if (max <= 0)
        {
            return items.Select(x => new DashboardChartPoint
            {
                Label = x.Label,
                Value = 12,
                Date = x.Date
            }).ToList();
        }

        return items.Select(x =>
        {
            var raw = Math.Max(0, SafeInt(x.Value));
            var scaled = Math.Max(12, (int)Math.Round((raw / (double)max) * 130d));

            return new DashboardChartPoint
            {
                Label = x.Label,
                Value = scaled,
                Date = x.Date
            };
        }).ToList();
    }

    private List<DashboardRecentActivityItem> BuildRecentActivities(
        IEnumerable<DashboardBookProgressItem> filteredBooks,
        IEnumerable<DashboardChartPoint> filteredWeeklyRaw)
    {
        var bookActivities = filteredBooks
            .Where(x => x.LastActivityAt.HasValue)
            .OrderByDescending(x => x.LastActivityAt)
            .Take(8)
            .Select(x => new DashboardRecentActivityItem
            {
                Title = x.Title,
                Subtitle = $"پیشرفت: {x.ProgressPercent:0.##}% | XP: {x.EarnedXp}",
                ActivityType = "book",
                DisplayType = "کتاب",
                ActivityAt = x.LastActivityAt,
                Accent = "#176B52"
            });

        var weeklyActivities = filteredWeeklyRaw
            .Where(x => x.Date.HasValue && SafeInt(x.Value) > 0)
            .OrderByDescending(x => x.Date)
            .Take(5)
            .Select(x => new DashboardRecentActivityItem
            {
                Title = $"فعالیت روزانه: {x.Label}",
                Subtitle = $"مقدار فعالیت ثبت‌شده: {SafeInt(x.Value)}",
                ActivityType = "study",
                DisplayType = "فعالیت",
                ActivityAt = x.Date,
                Accent = "#D6A84B"
            });

        return bookActivities
            .Concat(weeklyActivities)
            .OrderByDescending(x => x.ActivityAt)
            .Take(10)
            .ToList();
    }

    private (DateTime? Start, DateTime? End) GetSelectedDateRange()
    {
        var now = DateTime.Now;
        var today = now.Date;

        return SelectedFilterOption switch
        {
            "امروز" => (today, now),
            "۷ روز اخیر" => (today.AddDays(-6), now),
            "۳۰ روز اخیر" => (today.AddDays(-29), now),
            _ => (null, null)
        };
    }

    private static IEnumerable<T> ApplyDateFilter<T>(
        IEnumerable<T> items,
        Func<T, DateTime?> dateSelector,
        (DateTime? Start, DateTime? End) range)
    {
        if (range.Start is null || range.End is null)
            return items;

        return items.Where(item =>
        {
            var date = dateSelector(item);
            return date.HasValue &&
                   date.Value >= range.Start.Value &&
                   date.Value <= range.End.Value;
        });
    }

    private IEnumerable<DashboardBookProgressItem> DistinctFilteredBooks()
    {
        return TopBooks
            .Concat(RecentBooks)
            .GroupBy(x => new { x.Title, x.LastActivityAt, x.ProgressPercent, x.EarnedXp })
            .Select(g => g.First());
    }

    private void RaiseFilterDependentProperties()
    {
        OnPropertyChanged(nameof(FilterDescription));
        OnPropertyChanged(nameof(HasAnyBooks));
        OnPropertyChanged(nameof(HasNoData));
        OnPropertyChanged(nameof(FilteredBookCount));
        OnPropertyChanged(nameof(FilteredRecentCount));
        OnPropertyChanged(nameof(FilteredWeeklyPointCount));
        OnPropertyChanged(nameof(FilteredActivityCount));
        OnPropertyChanged(nameof(FilteredAverageProgressPercent));
        OnPropertyChanged(nameof(FilteredActivityValueTotal));
    }

    private void RaiseSectionProperties()
    {
        OnPropertyChanged(nameof(HasWeeklyActivitySection));
        OnPropertyChanged(nameof(HasTopBooksSection));
        OnPropertyChanged(nameof(HasRecentBooksSection));
        OnPropertyChanged(nameof(HasRecentActivitiesSection));
    }

    private static int SafeInt(double value)
    {
        return (int)Math.Round(value);
    }

    private static void ReplaceCollection<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
            target.Add(item);
    }
}

public partial class DashboardRecentActivityItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private string _activityType = string.Empty;

    [ObservableProperty]
    private string _displayType = string.Empty;

    [ObservableProperty]
    private DateTime? _activityAt;

    [ObservableProperty]
    private string _accent = "#176B52";
}


