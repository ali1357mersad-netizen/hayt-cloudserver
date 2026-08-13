using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class PersonalGoalsService : IPersonalGoalsService
{
    private readonly object _sync = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public PersonalGoalsSnapshot Current { get; private set; } =
        PersonalGoalsSnapshot.Empty;

    public event EventHandler? GoalsChanged;

    private string AppDirectory
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Hayt");
        }
    }

    private string StateFilePath =>
        Path.Combine(AppDirectory, "personal-goals.json");

    public PersonalGoalsSnapshot Load()
    {
        lock (_sync)
        {
            var state = LoadState();
            Current = BuildSnapshot(state.Goals);
            GoalsChanged?.Invoke(this, EventArgs.Empty);
            return Current;
        }
    }

    public PersonalGoalsSnapshot Evaluate(DashboardSnapshot? dashboardSnapshot)
    {
        lock (_sync)
        {
            var state = LoadState();

            int xp = ExtractIntValue(dashboardSnapshot, new[]
            {
                "TotalXp",
                "XP",
                "Xp",
                "EarnedXp"
            });

            int studyMinutes = ExtractIntValue(dashboardSnapshot, new[]
            {
                "StudyMinutes",
                "TotalStudyMinutes",
                "LearningMinutes",
                "Minutes"
            });

            int activeDays = ExtractIntValue(dashboardSnapshot, new[]
            {
                "ActiveDays",
                "TotalActiveDays",
                "CurrentStreakDays",
                "CurrentStreak"
            });

            int lessons = ExtractIntValue(dashboardSnapshot, new[]
            {
                "CompletedLessons",
                "LessonsCompleted",
                "LessonCount"
            });

            int quizzes = ExtractIntValue(dashboardSnapshot, new[]
            {
                "CompletedQuizzes",
                "QuizCount",
                "PassedQuizzes"
            });

            foreach (var goal in state.Goals)
            {
                if (!goal.IsEnabled)
                {
                    continue;
                }

                int value = goal.Metric switch
                {
                    PersonalGoalMetric.StudyMinutes => studyMinutes,
                    PersonalGoalMetric.ActiveDays => activeDays,
                    PersonalGoalMetric.Xp => xp,
                    PersonalGoalMetric.Lessons => lessons,
                    PersonalGoalMetric.Quizzes => quizzes,
                    _ => goal.CurrentValue
                };

                if (value > 0)
                {
                    goal.CurrentValue = Math.Max(value, 0);
                }

                RefreshCompletedAt(goal);
            }

            SaveState(state);

            Current = BuildSnapshot(state.Goals);
            GoalsChanged?.Invoke(this, EventArgs.Empty);

            return Current;
        }
    }

    public PersonalGoalsSnapshot UpdateGoalProgress(string key, int currentValue)
    {
        lock (_sync)
        {
            var state = LoadState();
            var goal = FindGoal(state, key);

            if (goal is not null)
            {
                goal.CurrentValue = Math.Max(currentValue, 0);
                RefreshCompletedAt(goal);
                SaveState(state);
            }

            return SetCurrentAndNotify(state);
        }
    }

    public PersonalGoalsSnapshot IncreaseGoalProgress(string key, int delta)
    {
        lock (_sync)
        {
            var state = LoadState();
            var goal = FindGoal(state, key);

            if (goal is not null)
            {
                goal.CurrentValue = Math.Max(goal.CurrentValue + delta, 0);
                RefreshCompletedAt(goal);
                SaveState(state);
            }

            return SetCurrentAndNotify(state);
        }
    }

    public PersonalGoalsSnapshot ResetGoalProgress(string key)
    {
        lock (_sync)
        {
            var state = LoadState();
            var goal = FindGoal(state, key);

            if (goal is not null)
            {
                goal.CurrentValue = 0;
                goal.CompletedAt = null;
                SaveState(state);
            }

            return SetCurrentAndNotify(state);
        }
    }

    public PersonalGoalsSnapshot SetGoalTarget(string key, int targetValue)
    {
        lock (_sync)
        {
            var state = LoadState();
            var goal = FindGoal(state, key);

            if (goal is not null)
            {
                goal.TargetValue = Math.Max(targetValue, 1);
                RefreshCompletedAt(goal);
                SaveState(state);
            }

            return SetCurrentAndNotify(state);
        }
    }

    public PersonalGoalsSnapshot IncreaseGoalTarget(string key, int delta)
    {
        lock (_sync)
        {
            var state = LoadState();
            var goal = FindGoal(state, key);

            if (goal is not null)
            {
                goal.TargetValue = Math.Max(goal.TargetValue + delta, 1);
                RefreshCompletedAt(goal);
                SaveState(state);
            }

            return SetCurrentAndNotify(state);
        }
    }

    public PersonalGoalsSnapshot ToggleGoalEnabled(string key)
    {
        lock (_sync)
        {
            var state = LoadState();
            var goal = FindGoal(state, key);

            if (goal is not null)
            {
                goal.IsEnabled = !goal.IsEnabled;
                RefreshCompletedAt(goal);
                SaveState(state);
            }

            return SetCurrentAndNotify(state);
        }
    }

    public PersonalGoalsSnapshot ResetToDefaults()
    {
        lock (_sync)
        {
            var state = PersonalGoalsState.CreateDefault();
            SaveState(state);

            return SetCurrentAndNotify(state);
        }
    }

    public IReadOnlyList<PersonalGoal> GetGoals()
    {
        return LoadState()
            .Goals
            .OrderBy(x => x.SortOrder)
            .ToArray();
    }

    private PersonalGoalsSnapshot SetCurrentAndNotify(PersonalGoalsState state)
    {
        Current = BuildSnapshot(state.Goals);
        GoalsChanged?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    private static PersonalGoal? FindGoal(PersonalGoalsState state, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return state.Goals.FirstOrDefault(
            x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static void RefreshCompletedAt(PersonalGoal goal)
    {
        if (!goal.IsEnabled)
        {
            goal.CompletedAt = null;
            return;
        }

        if (goal.IsCompleted && goal.CompletedAt is null)
        {
            goal.CompletedAt = DateTime.Now;
        }

        if (!goal.IsCompleted)
        {
            goal.CompletedAt = null;
        }
    }

    private PersonalGoalsSnapshot BuildSnapshot(IEnumerable<PersonalGoal> goals)
    {
        return new PersonalGoalsSnapshot
        {
            EvaluatedAt = DateTime.Now,
            Goals = goals
                .OrderBy(x => x.SortOrder)
                .ToArray()
        };
    }

    private PersonalGoalsState LoadState()
    {
        try
        {
            if (!Directory.Exists(AppDirectory))
            {
                Directory.CreateDirectory(AppDirectory);
            }

            if (!File.Exists(StateFilePath))
            {
                var defaultState = PersonalGoalsState.CreateDefault();
                SaveState(defaultState);
                return defaultState;
            }

            string json = File.ReadAllText(StateFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return PersonalGoalsState.CreateDefault();
            }

            var state = JsonSerializer.Deserialize<PersonalGoalsState>(json, _jsonOptions);

            if (state is null || state.Goals.Count == 0)
            {
                return PersonalGoalsState.CreateDefault();
            }

            EnsureDefaultGoals(state);
            return state;
        }
        catch
        {
            return PersonalGoalsState.CreateDefault();
        }
    }

    private void SaveState(PersonalGoalsState state)
    {
        state.LastSavedAt = DateTime.Now;

        if (!Directory.Exists(AppDirectory))
        {
            Directory.CreateDirectory(AppDirectory);
        }

        string json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(StateFilePath, json);
    }

    private void EnsureDefaultGoals(PersonalGoalsState state)
    {
        var defaults = PersonalGoalsState.CreateDefault();

        foreach (var defaultGoal in defaults.Goals)
        {
            bool exists = state.Goals.Any(
                x => string.Equals(x.Key, defaultGoal.Key, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                state.Goals.Add(defaultGoal);
            }
        }

        state.Goals = state.Goals
            .OrderBy(x => x.SortOrder)
            .ToList();
    }

    private static int ExtractIntValue(object? source, IEnumerable<string> candidateNames)
    {
        if (source is null)
        {
            return 0;
        }

        var candidates = candidateNames
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        int best = 0;
        ExtractIntValueRecursive(
            source,
            candidates,
            0,
            new HashSet<object>(ReferenceEqualityComparer.Instance),
            ref best);

        return best;
    }

    private static void ExtractIntValueRecursive(
        object? value,
        IReadOnlyList<string> candidateNames,
        int depth,
        ISet<object> visited,
        ref int best)
    {
        if (value is null || depth > 5)
        {
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
        }

        PropertyInfo[] properties;

        try
        {
            properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }
        catch
        {
            return;
        }

        foreach (var property in properties)
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
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

            string propertyName = property.Name.ToLowerInvariant();

            if (candidateNames.Any(x => propertyName.Contains(x)))
            {
                if (TryConvertToInt(propertyValue, out int intValue))
                {
                    best = Math.Max(best, intValue);
                }
            }

            if (propertyValue is not null && !IsSimpleType(propertyValue.GetType()))
            {
                ExtractIntValueRecursive(
                    propertyValue,
                    candidateNames,
                    depth + 1,
                    visited,
                    ref best);
            }
        }
    }

    private static bool TryConvertToInt(object? value, out int result)
    {
        result = 0;

        if (value is null)
        {
            return false;
        }

        switch (value)
        {
            case int i:
                result = i;
                return true;
            case long l:
                result = l > int.MaxValue ? int.MaxValue : (int)l;
                return true;
            case double d:
                result = (int)Math.Round(d);
                return true;
            case decimal m:
                result = (int)Math.Round(m);
                return true;
            case float f:
                result = (int)Math.Round(f);
                return true;
            default:
                return int.TryParse(value.ToString(), out result);
        }
    }

    private static bool IsSimpleType(Type type)
    {
        Type actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType.IsPrimitive ||
               actualType.IsEnum ||
               actualType == typeof(string) ||
               actualType == typeof(decimal) ||
               actualType == typeof(Guid) ||
               actualType == typeof(DateTime) ||
               actualType == typeof(DateTimeOffset) ||
               actualType == typeof(TimeSpan) ||
               actualType == typeof(Uri);
    }
}

