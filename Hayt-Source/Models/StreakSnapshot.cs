using System;
using System.Collections.Generic;

namespace Hayt.Models;

public sealed class StreakSnapshot
{
    public static StreakSnapshot Empty { get; } = new();

    public DateTime EvaluatedDate { get; init; } = DateTime.Today;

    public int CurrentStreak { get; init; }

    public int BestStreak { get; init; }

    public int TotalActiveDays { get; init; }

    public DateTime? FirstActivityDate { get; init; }

    public DateTime? LastActivityDate { get; init; }

    public bool HasActivityToday { get; init; }

    public bool HasActivityYesterday { get; init; }

    public IReadOnlyList<StreakMilestone> Milestones { get; init; } =
        Array.Empty<StreakMilestone>();

    public IReadOnlyList<StreakMilestone> NewlyUnlockedMilestones { get; init; } =
        Array.Empty<StreakMilestone>();

    public StreakMilestone? NextMilestone { get; init; }

    public bool HasMilestones =>
        Milestones.Count > 0;

    public bool HasUnlockedMilestones
    {
        get
        {
            foreach (var milestone in Milestones)
            {
                if (milestone.IsUnlocked)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool HasNextMilestone =>
        NextMilestone is not null;

    public bool IsActiveToday =>
        HasActivityToday && CurrentStreak > 0;

    public bool IsAtRisk =>
        !HasActivityToday &&
        HasActivityYesterday &&
        CurrentStreak > 0;

    public bool IsLost =>
        TotalActiveDays > 0 &&
        !HasActivityToday &&
        !HasActivityYesterday;

    public bool HasNoActivity =>
        TotalActiveDays == 0;

    public int DaysToBeatBest
    {
        get
        {
            if (BestStreak <= 0)
            {
                return 1;
            }

            if (CurrentStreak > BestStreak)
            {
                return 0;
            }

            return (BestStreak - CurrentStreak) + 1;
        }
    }

    public int DaysToNextMilestone
    {
        get
        {
            if (NextMilestone is null)
            {
                return 0;
            }

            return Math.Max(NextMilestone.RequiredDays - CurrentStreak, 0);
        }
    }

    public string NextMilestoneText
    {
        get
        {
            if (NextMilestone is null)
            {
                return "همه Milestoneهای فعلی آزاد شده‌اند";
            }

            int remaining = DaysToNextMilestone;

            if (remaining <= 0)
            {
                return $"Milestone بعدی آماده است: {NextMilestone.Title}";
            }

            return $"{remaining} روز تا {NextMilestone.Title}";
        }
    }

    public string StatusTitle
    {
        get
        {
            if (HasNoActivity)
            {
                return "هنوز زنجیره‌ای شروع نشده";
            }

            if (IsActiveToday)
            {
                return "زنجیره امروز فعال است";
            }

            if (IsAtRisk)
            {
                return "زنجیره در خطر است";
            }

            return "زنجیره شکسته شده";
        }
    }

    public string StatusMessage
    {
        get
        {
            if (HasNoActivity)
            {
                return "با اولین فعالیت، زنجیره یادگیری خود را شروع کن.";
            }

            if (IsActiveToday)
            {
                return CurrentStreak == 1
                    ? "شروع خوبی بود؛ فردا هم ادامه بده."
                    : $"{CurrentStreak} روز پیاپی فعال بوده‌ای.";
            }

            if (IsAtRisk)
            {
                return "امروز فعالیت کن تا زنجیره‌ات حفظ شود.";
            }

            return "از امروز یک زنجیره تازه شروع کن.";
        }
    }
}