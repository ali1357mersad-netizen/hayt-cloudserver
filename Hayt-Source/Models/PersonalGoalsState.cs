using System;
using System.Collections.Generic;

namespace Hayt.Models;

public sealed class PersonalGoalsState
{
    public DateTime LastSavedAt { get; set; } = DateTime.Now;

    public List<PersonalGoal> Goals { get; set; } = new();

    public static PersonalGoalsState CreateDefault()
    {
        return new PersonalGoalsState
        {
            LastSavedAt = DateTime.Now,
            Goals = new List<PersonalGoal>
            {
                new()
                {
                    Key = "daily_study_minutes",
                    Title = "مطالعه روزانه",
                    Description = "هر روز حداقل ۳۰ دقیقه مطالعه کن.",
                    Icon = "📚",
                    Period = PersonalGoalPeriod.Daily,
                    Metric = PersonalGoalMetric.StudyMinutes,
                    TargetValue = 30,
                    CurrentValue = 0,
                    IsEnabled = true,
                    SortOrder = 1
                },
                new()
                {
                    Key = "daily_xp",
                    Title = "XP روزانه",
                    Description = "امروز حداقل ۱۰۰ امتیاز XP کسب کن.",
                    Icon = "⭐",
                    Period = PersonalGoalPeriod.Daily,
                    Metric = PersonalGoalMetric.Xp,
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsEnabled = true,
                    SortOrder = 2
                },
                new()
                {
                    Key = "weekly_active_days",
                    Title = "فعالیت هفتگی",
                    Description = "در هفته حداقل ۵ روز فعال باش.",
                    Icon = "🔥",
                    Period = PersonalGoalPeriod.Weekly,
                    Metric = PersonalGoalMetric.ActiveDays,
                    TargetValue = 5,
                    CurrentValue = 0,
                    IsEnabled = true,
                    SortOrder = 3
                }
            }
        };
    }
}