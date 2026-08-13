using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class AchievementViewModel : ObservableObject
{
    private readonly IAchievementService _achievementService;

    public ObservableCollection<UserAchievementState> Achievements =>
        _achievementService.Achievements;

    public IEnumerable<UserAchievementState> UnlockedAchievements =>
        Achievements
            .Where(x => x.IsUnlocked)
            .OrderByDescending(x => x.UnlockedAt)
            .ThenBy(x => x.SortOrder);

    public IEnumerable<UserAchievementState> LockedAchievements =>
        Achievements
            .Where(x => !x.IsUnlocked)
            .OrderBy(x => x.SortOrder);

    public IEnumerable<UserAchievementState> RecentUnlocks =>
        Achievements
            .Where(x => x.IsUnlocked)
            .OrderByDescending(x => x.UnlockedAt)
            .ThenBy(x => x.SortOrder)
            .Take(3);

    public int TotalCount => Achievements.Count;

    public int UnlockedCount =>
        Achievements.Count(x => x.IsUnlocked);

    public int LockedCount =>
        Achievements.Count(x => !x.IsUnlocked);

    public double CompletionPercent { get => TotalCount == 0
            ? 0
            : Math.Round((UnlockedCount / (double)TotalCount) * 100d, 1); set { } }

    public string CompletionText =>
        TotalCount == 0
            ? "هنوز دستاوردی تعریف نشده است"
            : $"{UnlockedCount} از {TotalCount} دستاورد آزاد شده";

    public string CompletionPercentText =>
        $"{CompletionPercent:0.#}%";

    public bool HasAchievements =>
        TotalCount > 0;

    public bool HasUnlockedAchievements =>
        UnlockedCount > 0;

    public bool HasLockedAchievements =>
        LockedCount > 0;

    public bool HasRecentUnlocks =>
        RecentUnlocks.Any();

    public bool HasNoUnlockedAchievements =>
        !HasUnlockedAchievements;

    public bool HasNoLockedAchievements =>
        !HasLockedAchievements;

    public AchievementViewModel(IAchievementService achievementService)
    {
        _achievementService = achievementService ??
            throw new ArgumentNullException(nameof(achievementService));

        _achievementService.AchievementsChanged += OnAchievementsChanged;
        RaiseState();
    }

    public async Task EvaluateAsync(DashboardSnapshot? snapshot)
    {
        await _achievementService.EvaluateAsync(snapshot);
        RaiseState();
    }

    [RelayCommand]
    private void RefreshState()
    {
        RaiseState();
    }

    [RelayCommand]
    private void ResetRuntimeState()
    {
        _achievementService.ResetRuntimeState();
        RaiseState();
    }

    private void OnAchievementsChanged(object? sender, EventArgs e)
    {
        RaiseState();
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(Achievements));
        OnPropertyChanged(nameof(UnlockedAchievements));
        OnPropertyChanged(nameof(LockedAchievements));
        OnPropertyChanged(nameof(RecentUnlocks));

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(UnlockedCount));
        OnPropertyChanged(nameof(LockedCount));

        OnPropertyChanged(nameof(CompletionPercent));
        OnPropertyChanged(nameof(CompletionText));
        OnPropertyChanged(nameof(CompletionPercentText));

        OnPropertyChanged(nameof(HasAchievements));
        OnPropertyChanged(nameof(HasUnlockedAchievements));
        OnPropertyChanged(nameof(HasLockedAchievements));
        OnPropertyChanged(nameof(HasRecentUnlocks));
        OnPropertyChanged(nameof(HasNoUnlockedAchievements));
        OnPropertyChanged(nameof(HasNoLockedAchievements));
    }
}


