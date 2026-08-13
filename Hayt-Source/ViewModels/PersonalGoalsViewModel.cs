using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class PersonalGoalsViewModel : ObservableObject
{
    private readonly IPersonalGoalsService _goalsService;

    [ObservableProperty]
    private PersonalGoalsSnapshot _snapshot = PersonalGoalsSnapshot.Empty;

    public IReadOnlyList<PersonalGoal> Goals => Snapshot.Goals;

    public IReadOnlyList<PersonalGoal> DailyGoals => Snapshot.DailyGoals;

    public IReadOnlyList<PersonalGoal> WeeklyGoals => Snapshot.WeeklyGoals;

    public int TotalGoals => Snapshot.TotalGoals;

    public int EnabledGoals => Snapshot.EnabledGoals;

    public int DisabledGoals => Snapshot.DisabledGoals;

    public int CompletedGoals => Snapshot.CompletedGoals;

    public int InProgressGoals => Snapshot.InProgressGoals;

    public bool HasGoals => Snapshot.HasGoals;

    public bool HasCompletedGoals => Snapshot.HasCompletedGoals;

    public bool HasIncompleteGoals => Snapshot.HasIncompleteGoals;

    public double OverallProgressPercent => Snapshot.OverallProgressPercent;

    public string OverallProgressText => Snapshot.OverallProgressText;

    public string OverallPercentText => Snapshot.OverallPercentText;

    public string HeaderText =>
        HasGoals
            ? OverallProgressText
            : "هنوز هدفی تعریف نشده است";

    public PersonalGoalsViewModel(IPersonalGoalsService goalsService)
    {
        _goalsService = goalsService ??
            throw new ArgumentNullException(nameof(goalsService));

        Snapshot = _goalsService.Load();

        _goalsService.GoalsChanged += (_, _) =>
        {
            Snapshot = _goalsService.Current;
            RaiseState();
        };

        RaiseState();
    }

    public void Evaluate(DashboardSnapshot? dashboardSnapshot)
    {
        Snapshot = _goalsService.Evaluate(dashboardSnapshot);
        RaiseState();
    }

    [RelayCommand]
    private void RefreshState()
    {
        Snapshot = _goalsService.Load();
        RaiseState();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        Snapshot = _goalsService.ResetToDefaults();
        RaiseState();
    }

    [RelayCommand]
    private void IncreaseTarget(PersonalGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        int delta = GetDefaultDelta(goal);
        Snapshot = _goalsService.IncreaseGoalTarget(goal.Key, delta);
        RaiseState();
    }

    [RelayCommand]
    private void DecreaseTarget(PersonalGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        int delta = -GetDefaultDelta(goal);
        Snapshot = _goalsService.IncreaseGoalTarget(goal.Key, delta);
        RaiseState();
    }

    [RelayCommand]
    private void IncreaseProgress(PersonalGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        int delta = GetProgressDelta(goal);
        Snapshot = _goalsService.IncreaseGoalProgress(goal.Key, delta);
        RaiseState();
    }

    [RelayCommand]
    private void DecreaseProgress(PersonalGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        int delta = -GetProgressDelta(goal);
        Snapshot = _goalsService.IncreaseGoalProgress(goal.Key, delta);
        RaiseState();
    }

    [RelayCommand]
    private void ResetGoalProgress(PersonalGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        Snapshot = _goalsService.ResetGoalProgress(goal.Key);
        RaiseState();
    }

    [RelayCommand]
    private void ToggleGoalEnabled(PersonalGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        Snapshot = _goalsService.ToggleGoalEnabled(goal.Key);
        RaiseState();
    }

    partial void OnSnapshotChanged(PersonalGoalsSnapshot value)
    {
        RaiseState();
    }

    private static int GetDefaultDelta(PersonalGoal goal)
    {
        return goal.Metric switch
        {
            PersonalGoalMetric.StudyMinutes => 5,
            PersonalGoalMetric.ActiveDays => 1,
            PersonalGoalMetric.Xp => 25,
            PersonalGoalMetric.Lessons => 1,
            PersonalGoalMetric.Quizzes => 1,
            _ => 1
        };
    }

    private static int GetProgressDelta(PersonalGoal goal)
    {
        return goal.Metric switch
        {
            PersonalGoalMetric.StudyMinutes => 5,
            PersonalGoalMetric.ActiveDays => 1,
            PersonalGoalMetric.Xp => 10,
            PersonalGoalMetric.Lessons => 1,
            PersonalGoalMetric.Quizzes => 1,
            _ => 1
        };
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(Goals));
        OnPropertyChanged(nameof(DailyGoals));
        OnPropertyChanged(nameof(WeeklyGoals));

        OnPropertyChanged(nameof(TotalGoals));
        OnPropertyChanged(nameof(EnabledGoals));
        OnPropertyChanged(nameof(DisabledGoals));
        OnPropertyChanged(nameof(CompletedGoals));
        OnPropertyChanged(nameof(InProgressGoals));

        OnPropertyChanged(nameof(HasGoals));
        OnPropertyChanged(nameof(HasCompletedGoals));
        OnPropertyChanged(nameof(HasIncompleteGoals));

        OnPropertyChanged(nameof(OverallProgressPercent));
        OnPropertyChanged(nameof(OverallProgressText));
        OnPropertyChanged(nameof(OverallPercentText));
        OnPropertyChanged(nameof(HeaderText));
    }
}

