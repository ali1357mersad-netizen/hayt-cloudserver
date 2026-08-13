using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class StreakService : IStreakService
{
    private const int MaximumReflectionDepth = 6;
    private const int MaximumVisitedObjects = 2_000;

    private readonly INotificationService? _notificationService;
    private readonly IStreakPersistenceService? _persistenceService;

    private readonly HashSet<string> _notifiedMilestones =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] ActivityKeywords =
    {
        "activity",
        "active",
        "completed",
        "completion",
        "progress",
        "attempt",
        "answer",
        "quiz",
        "lesson",
        "study",
        "learn",
        "recent",
        "timeline",
        "event",
        "session",
        "lastaccess",
        "lastseen",
        "فعالیت",
        "مطالعه",
        "یادگیری",
        "درس",
        "آزمون",
        "پیشرفت"
    };

    private static readonly string[] ExcludedKeywords =
    {
        "createdat",
        "createddate",
        "generated",
        "refreshed",
        "updatedat",
        "snapshotdate",
        "evaluated",
        "notification",
        "achievement",
        "unlock",
        "certificate",
        "birth",
        "expire",
        "migration"
    };

    private static readonly IReadOnlyList<StreakMilestone> StaticMilestoneDefinitions =
        new List<StreakMilestone>
        {
            new()
            {
                Key = "streak_3_days",
                Title = "جرقه ۳ روزه",
                Description = "۳ روز پیاپی فعال ماندی.",
                Icon = "🔥",
                RequiredDays = 3
            },
            new()
            {
                Key = "streak_7_days",
                Title = "هفته آتشین",
                Description = "۷ روز پیاپی یادگیری را ادامه دادی.",
                Icon = "🔥",
                RequiredDays = 7
            },
            new()
            {
                Key = "streak_14_days",
                Title = "دو هفته پایدار",
                Description = "۱۴ روز پیاپی زنجیره یادگیری را حفظ کردی.",
                Icon = "⚡",
                RequiredDays = 14
            },
            new()
            {
                Key = "streak_30_days",
                Title = "ماه طلایی",
                Description = "۳۰ روز پیاپی فعال ماندی.",
                Icon = "🏆",
                RequiredDays = 30
            },
            new()
            {
                Key = "streak_100_days",
                Title = "افسانه ۱۰۰ روزه",
                Description = "۱۰۰ روز پیاپی یادگیری؛ فوق‌العاده است.",
                Icon = "👑",
                RequiredDays = 100
            }
        };

    public StreakSnapshot Current { get; private set; } =
        StreakSnapshot.Empty;

    public event EventHandler? StreakChanged;

    public IReadOnlyList<StreakMilestone> MilestoneDefinitions =>
        StaticMilestoneDefinitions;

    public StreakService()
    {
        LoadPersistedNotifiedMilestones();
    }

    public StreakService(INotificationService notificationService)
    {
        _notificationService = notificationService;
        LoadPersistedNotifiedMilestones();
    }

    public StreakService(IStreakPersistenceService persistenceService)
    {
        _persistenceService = persistenceService;
        LoadPersistedNotifiedMilestones();
    }

    public StreakService(
        INotificationService notificationService,
        IStreakPersistenceService persistenceService)
    {
        _notificationService = notificationService;
        _persistenceService = persistenceService;
        LoadPersistedNotifiedMilestones();
    }

    public StreakSnapshot Calculate(
        IEnumerable<DateTime> activityDates,
        DateTime? referenceDate = null)
    {
        ArgumentNullException.ThrowIfNull(activityDates);

        DateTime today = NormalizeReferenceDate(referenceDate);

        DateTime[] uniqueDates = activityDates
            .Where(IsUsableDate)
            .Select(x => x.ToLocalTime().Date)
            .Where(x => x <= today)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        StreakSnapshot result = BuildSnapshot(uniqueDates, today);

        NotifyNewMilestones(result);
        PersistSnapshot(result);
        SetCurrent(result);

        return result;
    }

    public StreakSnapshot Evaluate(
        DashboardSnapshot? dashboardSnapshot,
        DateTime? referenceDate = null)
    {
        if (dashboardSnapshot is null)
        {
            return Calculate(Array.Empty<DateTime>(), referenceDate);
        }

        var dates = new List<DateTime>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int visitedCount = 0;

        ExtractActivityDates(
            dashboardSnapshot,
            dashboardSnapshot.GetType().Name,
            depth: 0,
            dates,
            visited,
            ref visitedCount);

        return Calculate(dates, referenceDate);
    }

    public void ResetRuntimeState()
    {
        _notifiedMilestones.Clear();

        if (_persistenceService is not null)
        {
            _persistenceService.Reset();
        }

        SetCurrent(StreakSnapshot.Empty);
    }

    private void LoadPersistedNotifiedMilestones()
    {
        if (_persistenceService is null)
        {
            return;
        }

        foreach (string key in _persistenceService.GetNotifiedMilestoneKeys())
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _notifiedMilestones.Add(key);
            }
        }
    }

    private StreakSnapshot BuildSnapshot(
        IReadOnlyList<DateTime> dates,
        DateTime today)
    {
        if (dates.Count == 0)
        {
            var emptyMilestones = BuildMilestones(0, today);

            return new StreakSnapshot
            {
                EvaluatedDate = today,
                CurrentStreak = 0,
                BestStreak = 0,
                TotalActiveDays = 0,
                FirstActivityDate = null,
                LastActivityDate = null,
                HasActivityToday = false,
                HasActivityYesterday = false,
                Milestones = emptyMilestones,
                NewlyUnlockedMilestones = Array.Empty<StreakMilestone>(),
                NextMilestone = FindNextMilestone(emptyMilestones)
            };
        }

        DateTime yesterday = today.AddDays(-1);
        bool hasToday = dates.Contains(today);
        bool hasYesterday = dates.Contains(yesterday);

        int bestStreak = CalculateBestStreak(dates);
        int currentStreak = CalculateCurrentStreak(
            dates,
            today,
            hasToday,
            hasYesterday);

        var milestones = BuildMilestones(currentStreak, today);

        var newlyUnlocked = milestones
            .Where(x => x.IsUnlocked && !_notifiedMilestones.Contains(x.Key))
            .OrderBy(x => x.RequiredDays)
            .ToArray();

        return new StreakSnapshot
        {
            EvaluatedDate = today,
            CurrentStreak = currentStreak,
            BestStreak = bestStreak,
            TotalActiveDays = dates.Count,
            FirstActivityDate = dates[0],
            LastActivityDate = dates[^1],
            HasActivityToday = hasToday,
            HasActivityYesterday = hasYesterday,
            Milestones = milestones,
            NewlyUnlockedMilestones = newlyUnlocked,
            NextMilestone = FindNextMilestone(milestones)
        };
    }

    private IReadOnlyList<StreakMilestone> BuildMilestones(
        int currentStreak,
        DateTime today)
    {
        return StaticMilestoneDefinitions
            .OrderBy(x => x.RequiredDays)
            .Select(def =>
            {
                bool unlocked = currentStreak >= def.RequiredDays;

                return new StreakMilestone
                {
                    Key = def.Key,
                    Title = def.Title,
                    Description = def.Description,
                    Icon = def.Icon,
                    RequiredDays = def.RequiredDays,
                    IsUnlocked = unlocked,
                    UnlockedAt = unlocked ? today : null
                };
            })
            .ToArray();
    }

    private static StreakMilestone? FindNextMilestone(
        IEnumerable<StreakMilestone> milestones)
    {
        return milestones
            .Where(x => !x.IsUnlocked)
            .OrderBy(x => x.RequiredDays)
            .FirstOrDefault();
    }

    private void NotifyNewMilestones(StreakSnapshot snapshot)
    {
        foreach (var milestone in snapshot.NewlyUnlockedMilestones)
        {
            if (!_notifiedMilestones.Add(milestone.Key))
            {
                continue;
            }

            _persistenceService?.MarkMilestoneNotified(milestone.Key);

            _notificationService?.AddNotification(
                $"Milestone جدید: {milestone.Title}",
                milestone.Description,
                NotificationType.Achievement);
        }
    }

    private void PersistSnapshot(StreakSnapshot snapshot)
    {
        _persistenceService?.SaveFromSnapshot(snapshot, _notifiedMilestones);
    }

    private static int CalculateBestStreak(
        IReadOnlyList<DateTime> dates)
    {
        if (dates.Count == 0)
        {
            return 0;
        }

        int best = 1;
        int running = 1;

        for (int index = 1; index < dates.Count; index++)
        {
            double dayDifference =
                (dates[index] - dates[index - 1]).TotalDays;

            if (dayDifference == 1d)
            {
                running++;
                best = Math.Max(best, running);
            }
            else
            {
                running = 1;
            }
        }

        return best;
    }

    private static int CalculateCurrentStreak(
        IReadOnlyList<DateTime> dates,
        DateTime today,
        bool hasToday,
        bool hasYesterday)
    {
        if (!hasToday && !hasYesterday)
        {
            return 0;
        }

        DateTime expectedDate = hasToday
            ? today
            : today.AddDays(-1);

        var dateSet = dates.ToHashSet();
        int current = 0;

        while (dateSet.Contains(expectedDate))
        {
            current++;
            expectedDate = expectedDate.AddDays(-1);
        }

        return current;
    }

    private static DateTime NormalizeReferenceDate(
        DateTime? referenceDate)
    {
        DateTime value = referenceDate ?? DateTime.Now;

        if (value.Kind == DateTimeKind.Utc)
        {
            value = value.ToLocalTime();
        }

        return value.Date;
    }

    private static bool IsUsableDate(DateTime value)
    {
        if (value == default)
        {
            return false;
        }

        int minimumYear = DateTime.Now.Year - 30;
        int maximumYear = DateTime.Now.Year + 1;

        return value.Year >= minimumYear &&
               value.Year <= maximumYear;
    }

    private static void ExtractActivityDates(
        object? value,
        string propertyPath,
        int depth,
        ICollection<DateTime> dates,
        ISet<object> visited,
        ref int visitedCount)
    {
        if (value is null ||
            depth > MaximumReflectionDepth ||
            visitedCount >= MaximumVisitedObjects)
        {
            return;
        }

        if (value is DateTime dateTime)
        {
            if (IsActivityPath(propertyPath) && IsUsableDate(dateTime))
            {
                dates.Add(dateTime);
            }

            return;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            if (IsActivityPath(propertyPath))
            {
                DateTime localDate = dateTimeOffset.LocalDateTime;

                if (IsUsableDate(localDate))
                {
                    dates.Add(localDate);
                }
            }

            return;
        }

        Type type = value.GetType();

        if (IsSimpleType(type))
        {
            return;
        }

        if (!type.IsValueType)
        {
            if (!visited.Add(value))
            {
                return;
            }

            visitedCount++;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            int itemIndex = 0;

            foreach (object? item in enumerable)
            {
                if (itemIndex >= 1_000)
                {
                    break;
                }

                ExtractActivityDates(
                    item,
                    $"{propertyPath}[{itemIndex}]",
                    depth + 1,
                    dates,
                    visited,
                    ref visitedCount);

                itemIndex++;
            }

            return;
        }

        PropertyInfo[] properties;

        try
        {
            properties = type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public);
        }
        catch
        {
            return;
        }

        foreach (PropertyInfo property in properties)
        {
            if (!property.CanRead ||
                property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? propertyValue;

            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            string childPath =
                $"{propertyPath}.{property.Name}";

            ExtractActivityDates(
                propertyValue,
                childPath,
                depth + 1,
                dates,
                visited,
                ref visitedCount);
        }
    }

    private static bool IsActivityPath(string propertyPath)
    {
        string normalized = propertyPath
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();

        if (ExcludedKeywords.Any(normalized.Contains))
        {
            return false;
        }

        return ActivityKeywords.Any(normalized.Contains);
    }

    private static bool IsSimpleType(Type type)
    {
        Type actualType =
            Nullable.GetUnderlyingType(type) ?? type;

        return actualType.IsPrimitive ||
               actualType.IsEnum ||
               actualType == typeof(string) ||
               actualType == typeof(decimal) ||
               actualType == typeof(Guid) ||
               actualType == typeof(TimeSpan) ||
               actualType == typeof(Uri);
    }

    private void SetCurrent(StreakSnapshot value)
    {
        Current = value;
        StreakChanged?.Invoke(this, EventArgs.Empty);
    }
}

