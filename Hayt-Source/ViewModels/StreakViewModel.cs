using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class StreakViewModel : ObservableObject
{
    private readonly IStreakService _streakService;

    [ObservableProperty]
    private StreakSnapshot _snapshot = StreakSnapshot.Empty;

    public int CurrentStreak => Snapshot.CurrentStreak;
    public int BestStreak => Snapshot.BestStreak;
    public int TotalActiveDays => Snapshot.TotalActiveDays;
    public int DaysToBeatBest => Snapshot.DaysToBeatBest;
    public int DaysToNextMilestone => Snapshot.DaysToNextMilestone;

    public bool HasNoActivity => Snapshot.HasNoActivity;
    public bool IsActiveToday => Snapshot.IsActiveToday;
    public bool IsAtRisk => Snapshot.IsAtRisk;
    public bool IsLost => Snapshot.IsLost;

    public bool HasMilestones => Snapshot.HasMilestones;
    public bool HasUnlockedMilestones => Snapshot.HasUnlockedMilestones;
    public bool HasNextMilestone => Snapshot.HasNextMilestone;

    public IReadOnlyList<StreakMilestone> Milestones => Snapshot.Milestones;
    public IReadOnlyList<StreakMilestone> NewlyUnlockedMilestones => Snapshot.NewlyUnlockedMilestones;
    public StreakMilestone? NextMilestone => Snapshot.NextMilestone;

    public string StatusTitle => Snapshot.StatusTitle;
    public string StatusMessage => Snapshot.StatusMessage;
    public string NextMilestoneText => Snapshot.NextMilestoneText;

    public string CurrentStreakText =>
        CurrentStreak == 0
            ? "۰ روز"
            : $"{CurrentStreak} روز";

    public string BestStreakText =>
        BestStreak == 0
            ? "هنوز رکوردی ثبت نشده"
            : $"{BestStreak} روز";

    public string TotalActiveDaysText =>
        TotalActiveDays == 0
            ? "بدون فعالیت"
            : $"{TotalActiveDays} روز فعال";

    public string DaysToBeatBestText =>
        BestStreak <= 0
            ? "با ۱ روز فعالیت اولین رکورد را بساز"
            : DaysToBeatBest <= 0
                ? "رکورد جدید ثبت شده است"
                : $"{DaysToBeatBest} روز تا رکورد جدید";

    public string StateEmoji =>
        HasNoActivity
            ? "🌱"
            : IsActiveToday
                ? "🔥"
                : IsAtRisk
                    ? "⚠️"
                    : "🔁";

    public string StateBadgeText =>
        HasNoActivity
            ? "شروع‌نشده"
            : IsActiveToday
                ? "فعال"
                : IsAtRisk
                    ? "در خطر"
                    : "شکسته";

    public string StateAccent =>
        HasNoActivity
            ? "#4B6B6A"
            : IsActiveToday
                ? "#176B52"
                : IsAtRisk
                    ? "#D6A84B"
                    : "#9A4A4A";

    public string LastActivityText =>
        Snapshot.LastActivityDate is null
            ? "آخرین فعالیت: ثبت نشده"
            : $"آخرین فعالیت: {Snapshot.LastActivityDate:yyyy/MM/dd}";

    public string FirstActivityText =>
        Snapshot.FirstActivityDate is null
            ? "اولین فعالیت: ثبت نشده"
            : $"اولین فعالیت: {Snapshot.FirstActivityDate:yyyy/MM/dd}";

    public StreakViewModel(IStreakService streakService)
    {
        _streakService = streakService ??
            throw new ArgumentNullException(nameof(streakService));

        Snapshot = _streakService.Current;

        _streakService.StreakChanged += (_, _) =>
        {
            Snapshot = _streakService.Current;
            RaiseState();
        };
    }

    public void Evaluate(DashboardSnapshot? dashboardSnapshot)
    {
        Snapshot = _streakService.Evaluate(dashboardSnapshot);
        RaiseState();
    }

    [RelayCommand]
    private void RefreshState()
    {
        Snapshot = _streakService.Current;
        RaiseState();
    }

    [RelayCommand]
    private void ResetRuntimeState()
    {
        _streakService.ResetRuntimeState();
        Snapshot = _streakService.Current;
        RaiseState();
    }

    partial void OnSnapshotChanged(StreakSnapshot value)
    {
        RaiseState();
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(CurrentStreak));
        OnPropertyChanged(nameof(BestStreak));
        OnPropertyChanged(nameof(TotalActiveDays));
        OnPropertyChanged(nameof(DaysToBeatBest));
        OnPropertyChanged(nameof(DaysToNextMilestone));

        OnPropertyChanged(nameof(HasNoActivity));
        OnPropertyChanged(nameof(IsActiveToday));
        OnPropertyChanged(nameof(IsAtRisk));
        OnPropertyChanged(nameof(IsLost));

        OnPropertyChanged(nameof(HasMilestones));
        OnPropertyChanged(nameof(HasUnlockedMilestones));
        OnPropertyChanged(nameof(HasNextMilestone));

        OnPropertyChanged(nameof(Milestones));
        OnPropertyChanged(nameof(NewlyUnlockedMilestones));
        OnPropertyChanged(nameof(NextMilestone));

        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(NextMilestoneText));

        OnPropertyChanged(nameof(CurrentStreakText));
        OnPropertyChanged(nameof(BestStreakText));
        OnPropertyChanged(nameof(TotalActiveDaysText));
        OnPropertyChanged(nameof(DaysToBeatBestText));

        OnPropertyChanged(nameof(StateEmoji));
        OnPropertyChanged(nameof(StateBadgeText));
        OnPropertyChanged(nameof(StateAccent));

        OnPropertyChanged(nameof(LastActivityText));
        OnPropertyChanged(nameof(FirstActivityText));
    }
}

