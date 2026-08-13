using System;
using System.Collections.Generic;

namespace Hayt.Models;

/// <summary>
/// وضعیت ذخیره‌شده Streak.
/// این مدل در فایل JSON ذخیره می‌شود و دیتابیس را تغییر نمی‌دهد.
/// </summary>
public sealed class StreakPersistenceState
{
    public DateTime LastSavedAt { get; set; } = DateTime.Now;

    public int CurrentStreak { get; set; }

    public int BestStreak { get; set; }

    public int TotalActiveDays { get; set; }

    public DateTime? FirstActivityDate { get; set; }

    public DateTime? LastActivityDate { get; set; }

    public List<string> NotifiedMilestoneKeys { get; set; } = new();

    public List<string> UnlockedMilestoneKeys { get; set; } = new();

    public static StreakPersistenceState Empty()
    {
        return new StreakPersistenceState
        {
            LastSavedAt = DateTime.Now,
            CurrentStreak = 0,
            BestStreak = 0,
            TotalActiveDays = 0,
            FirstActivityDate = null,
            LastActivityDate = null,
            NotifiedMilestoneKeys = new List<string>(),
            UnlockedMilestoneKeys = new List<string>()
        };
    }
}