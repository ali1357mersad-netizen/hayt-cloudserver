using System;

namespace Hayt.Models;

public enum PersonalGoalPeriod
{
    Daily = 0,
    Weekly = 1
}

public enum PersonalGoalMetric
{
    StudyMinutes = 0,
    ActiveDays = 1,
    Xp = 2,
    Lessons = 3,
    Quizzes = 4
}

public enum PersonalGoalStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Disabled = 3
}

public sealed class PersonalGoal
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = "🎯";

    public PersonalGoalPeriod Period { get; set; }

    public PersonalGoalMetric Metric { get; set; }

    public int TargetValue { get; set; }

    public int CurrentValue { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }

    public int SortOrder { get; set; }

    public double ProgressPercent
    {
        get
        {
            if (!IsEnabled || TargetValue <= 0)
            {
                return 0;
            }

            double value = (CurrentValue / (double)TargetValue) * 100d;
            return Math.Round(Math.Clamp(value, 0d, 100d), 1);
        }
    }

    public bool IsCompleted =>
        IsEnabled && ProgressPercent >= 100d;

    public int RemainingValue =>
        Math.Max(TargetValue - CurrentValue, 0);

    public PersonalGoalStatus Status
    {
        get
        {
            if (!IsEnabled)
            {
                return PersonalGoalStatus.Disabled;
            }

            if (IsCompleted)
            {
                return PersonalGoalStatus.Completed;
            }

            if (CurrentValue <= 0)
            {
                return PersonalGoalStatus.NotStarted;
            }

            return PersonalGoalStatus.InProgress;
        }
    }

    public string ProgressText =>
        $"{CurrentValue} / {TargetValue}";

    public string PercentText =>
        $"{ProgressPercent:0.#}%";

    public string PeriodText =>
        Period == PersonalGoalPeriod.Daily ? "روزانه" : "هفتگی";

    public string MetricText
    {
        get
        {
            return Metric switch
            {
                PersonalGoalMetric.StudyMinutes => "دقیقه مطالعه",
                PersonalGoalMetric.ActiveDays => "روز فعال",
                PersonalGoalMetric.Xp => "XP",
                PersonalGoalMetric.Lessons => "درس",
                PersonalGoalMetric.Quizzes => "آزمون",
                _ => "هدف"
            };
        }
    }

    public string StatusText
    {
        get
        {
            return Status switch
            {
                PersonalGoalStatus.Completed => "انجام‌شده",
                PersonalGoalStatus.InProgress => "در جریان",
                PersonalGoalStatus.Disabled => "غیرفعال",
                _ => "شروع‌نشده"
            };
        }
    }

    public string EnabledText =>
        IsEnabled ? "فعال" : "غیرفعال";

    public string ToggleText =>
        IsEnabled ? "غیرفعال کن" : "فعال کن";

    public string SummaryText =>
        $"{PeriodText} • {MetricText}";
}