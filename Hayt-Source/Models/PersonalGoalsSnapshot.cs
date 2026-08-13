using System;
using System.Collections.Generic;
using System.Linq;

namespace Hayt.Models;

public sealed class PersonalGoalsSnapshot
{
    public static PersonalGoalsSnapshot Empty { get; } = new();

    public DateTime EvaluatedAt { get; init; } = DateTime.Now;

    public IReadOnlyList<PersonalGoal> Goals { get; init; } =
        Array.Empty<PersonalGoal>();

    public IReadOnlyList<PersonalGoal> DailyGoals =>
        Goals
            .Where(x => x.Period == PersonalGoalPeriod.Daily)
            .OrderBy(x => x.SortOrder)
            .ToArray();

    public IReadOnlyList<PersonalGoal> WeeklyGoals =>
        Goals
            .Where(x => x.Period == PersonalGoalPeriod.Weekly)
            .OrderBy(x => x.SortOrder)
            .ToArray();

    public int TotalGoals =>
        Goals.Count;

    public int EnabledGoals =>
        Goals.Count(x => x.IsEnabled);

    public int DisabledGoals =>
        Goals.Count(x => !x.IsEnabled);

    public int CompletedGoals =>
        Goals.Count(x => x.IsEnabled && x.IsCompleted);

    public int InProgressGoals =>
        Goals.Count(x => x.IsEnabled && x.Status == PersonalGoalStatus.InProgress);

    public bool HasGoals =>
        TotalGoals > 0;

    public bool HasCompletedGoals =>
        CompletedGoals > 0;

    public bool HasIncompleteGoals =>
        EnabledGoals > CompletedGoals;

    public double OverallProgressPercent
    {
        get
        {
            var enabled = Goals
                .Where(x => x.IsEnabled)
                .ToArray();

            if (enabled.Length == 0)
            {
                return 0;
            }

            return Math.Round(enabled.Average(x => x.ProgressPercent), 1);
        }
    }

    public string OverallProgressText =>
        EnabledGoals == 0
            ? "هنوز هدف فعالی وجود ندارد"
            : $"{CompletedGoals} از {EnabledGoals} هدف انجام شده";

    public string OverallPercentText =>
        $"{OverallProgressPercent:0.#}%";
}