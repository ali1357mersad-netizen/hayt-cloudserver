using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public class AchievementService : IAchievementService
{
    private readonly INotificationService _notificationService;

    public ObservableCollection<UserAchievementState> Achievements { get; } = new();

    public event EventHandler? AchievementsChanged;

    public AchievementService(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        SeedDefinitions();
    }

    public Task EvaluateAsync(DashboardSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            RaiseChanged();
            return Task.CompletedTask;
        }

        UpdateProgressTexts(snapshot);

        UnlockIf("dashboard_first_open", true);
        UnlockIf("first_book_started", snapshot.Books?.Any() == true);
        UnlockIf("first_xp_earned", snapshot.Summary?.TotalXp > 0);
        UnlockIf("first_recent_activity", HasRecentActivity(snapshot));
        UnlockIf("streak_7_days", snapshot.Summary?.CurrentStreakDays >= 7);
        UnlockIf("streak_14_days", snapshot.Summary?.CurrentStreakDays >= 14);
        UnlockIf("streak_30_days", snapshot.Summary?.CurrentStreakDays >= 30);

        RaiseChanged();
        return Task.CompletedTask;
    }

    public void ResetRuntimeState()
    {
        foreach (var item in Achievements)
        {
            item.IsUnlocked = false;
            item.UnlockedAt = null;
            item.ProgressText = "قفل است";
        }

        RaiseChanged();
    }

    private void SeedDefinitions()
    {
        if (Achievements.Count > 0)
            return;

        var definitions = new List<AchievementDefinition>
        {
            new()
            {
                Key = "dashboard_first_open",
                Title = "اولین نگاه",
                Description = "برای اولین بار داشبورد را باز کردی.",
                Icon = "👀",
                SortOrder = 1
            },
            new()
            {
                Key = "first_book_started",
                Title = "اولین کتاب فعال",
                Description = "حداقل یک کتاب فعال در سیستم داری.",
                Icon = "📘",
                SortOrder = 2
            },
            new()
            {
                Key = "first_xp_earned",
                Title = "اولین XP",
                Description = "برای اولین بار XP کسب کردی.",
                Icon = "⭐",
                SortOrder = 3
            },
            new()
            {
                Key = "first_recent_activity",
                Title = "اولین فعالیت",
                Description = "حداقل یک فعالیت اخیر برایت ثبت شده است.",
                Icon = "⚡",
                SortOrder = 4
            },
            new()
            {
                Key = "streak_7_days",
                Title = "استریک ۷ روزه",
                Description = "۷ روز پیاپی فعال مانده‌ای.",
                Icon = "🔥",
                SortOrder = 5
            },
            new()
            {
                Key = "streak_14_days",
                Title = "استریک ۱۴ روزه",
                Description = "۱۴ روز پیاپی فعال مانده‌ای.",
                Icon = "⚡",
                SortOrder = 6
            },
            new()
            {
                Key = "streak_30_days",
                Title = "استریک ۳۰ روزه",
                Description = "۳۰ روز پیاپی یادگیری را ادامه داده‌ای.",
                Icon = "🏆",
                SortOrder = 7
            }
        };

        foreach (var def in definitions.OrderBy(x => x.SortOrder))
        {
            Achievements.Add(new UserAchievementState
            {
                Key = def.Key,
                Title = def.Title,
                Description = def.Description,
                Icon = def.Icon,
                SortOrder = def.SortOrder,
                IsUnlocked = false,
                UnlockedAt = null,
                ProgressText = "قفل است"
            });
        }

        RaiseChanged();
    }

    private void UpdateProgressTexts(DashboardSnapshot snapshot)
    {
        SetProgress(
            "dashboard_first_open",
            "با اولین بارگذاری داشبورد آزاد می‌شود.");

        SetProgress(
            "first_book_started",
            snapshot.Books?.Any() == true
                ? $"کتاب فعال: {snapshot.Books.Count()}"
                : "هنوز کتاب فعالی پیدا نشد.");

        SetProgress(
            "first_xp_earned",
            $"XP فعلی: {snapshot.Summary?.TotalXp ?? 0}");

        var recentCount =
            (snapshot.Books?.Count(x => x.LastActivityAt.HasValue) ?? 0) +
            (snapshot.WeeklyActivity?.Count(x => x.Date.HasValue && x.Value > 0) ?? 0);

        SetProgress(
            "first_recent_activity",
            $"فعالیت‌های قابل‌تشخیص: {recentCount}");

        SetProgress(
            "streak_7_days",
            $"استریک فعلی: {snapshot.Summary?.CurrentStreakDays ?? 0} / 7");
    }

    private bool HasRecentActivity(DashboardSnapshot snapshot)
    {
        var hasBookActivity = snapshot.Books?.Any(x => x.LastActivityAt.HasValue) == true;
        var hasWeeklyActivity = snapshot.WeeklyActivity?.Any(x => x.Date.HasValue && x.Value > 0) == true;
        return hasBookActivity || hasWeeklyActivity;
    }

    private void SetProgress(string key, string progressText)
    {
        var item = Achievements.FirstOrDefault(x => x.Key == key);
        if (item is null)
            return;

        if (!item.IsUnlocked)
            item.ProgressText = progressText;
    }

    private void UnlockIf(string key, bool condition)
    {
        if (!condition)
            return;

        var item = Achievements.FirstOrDefault(x => x.Key == key);
        if (item is null || item.IsUnlocked)
            return;

        item.IsUnlocked = true;
        item.UnlockedAt = DateTime.Now;
        item.ProgressText = "آزاد شد";

        _notificationService.AddNotification(
            $"دستاورد جدید: {item.Title}",
            item.Description,
            NotificationType.Achievement);
    }

    private void RaiseChanged()
    {
        AchievementsChanged?.Invoke(this, EventArgs.Empty);
    }
}

