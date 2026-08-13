using System;
using System.Collections.Generic;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IPersonalGoalsService
{
    PersonalGoalsSnapshot Current { get; }

    event EventHandler? GoalsChanged;

    PersonalGoalsSnapshot Load();

    PersonalGoalsSnapshot Evaluate(DashboardSnapshot? dashboardSnapshot);

    PersonalGoalsSnapshot UpdateGoalProgress(string key, int currentValue);

    PersonalGoalsSnapshot IncreaseGoalProgress(string key, int delta);

    PersonalGoalsSnapshot ResetGoalProgress(string key);

    PersonalGoalsSnapshot SetGoalTarget(string key, int targetValue);

    PersonalGoalsSnapshot IncreaseGoalTarget(string key, int delta);

    PersonalGoalsSnapshot ToggleGoalEnabled(string key);

    PersonalGoalsSnapshot ResetToDefaults();

    IReadOnlyList<PersonalGoal> GetGoals();
}

