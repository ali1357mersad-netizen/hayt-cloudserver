using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

/// <summary>
/// ذخیره دائمی وضعیت Streak در فایل JSON.
/// مسیر فایل:
/// %AppData%\Hayt\streak-state.json
/// </summary>
public sealed class StreakPersistenceService : IStreakPersistenceService
{
    private readonly object _sync = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private string AppDirectory
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Hayt");
        }
    }

    private string StateFilePath =>
        Path.Combine(AppDirectory, "streak-state.json");

    public StreakPersistenceState Load()
    {
        lock (_sync)
        {
            try
            {
                if (!Directory.Exists(AppDirectory))
                {
                    Directory.CreateDirectory(AppDirectory);
                }

                if (!File.Exists(StateFilePath))
                {
                    var empty = StreakPersistenceState.Empty();
                    SaveInternal(empty);
                    return empty;
                }

                string json = File.ReadAllText(StateFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return StreakPersistenceState.Empty();
                }

                var state = JsonSerializer.Deserialize<StreakPersistenceState>(json, _jsonOptions);

                return state ?? StreakPersistenceState.Empty();
            }
            catch
            {
                return StreakPersistenceState.Empty();
            }
        }
    }

    public void Save(StreakPersistenceState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_sync)
        {
            state.LastSavedAt = DateTime.Now;
            SaveInternal(state);
        }
    }

    public void SaveFromSnapshot(StreakSnapshot snapshot, IEnumerable<string> notifiedMilestoneKeys)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(notifiedMilestoneKeys);

        var notified = notifiedMilestoneKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unlocked = snapshot.Milestones?
            .Where(x => x.IsUnlocked)
            .Select(x => x.Key)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var state = new StreakPersistenceState
        {
            LastSavedAt = DateTime.Now,
            CurrentStreak = snapshot.CurrentStreak,
            BestStreak = snapshot.BestStreak,
            TotalActiveDays = snapshot.TotalActiveDays,
            FirstActivityDate = snapshot.FirstActivityDate,
            LastActivityDate = snapshot.LastActivityDate,
            NotifiedMilestoneKeys = notified,
            UnlockedMilestoneKeys = unlocked
        };

        Save(state);
    }

    public bool IsMilestoneNotified(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var state = Load();

        return state.NotifiedMilestoneKeys
            .Any(x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase));
    }

    public void MarkMilestoneNotified(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        lock (_sync)
        {
            var state = Load();

            if (!state.NotifiedMilestoneKeys.Any(x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase)))
            {
                state.NotifiedMilestoneKeys.Add(key);
            }

            Save(state);
        }
    }

    public IReadOnlyCollection<string> GetNotifiedMilestoneKeys()
    {
        return Load()
            .NotifiedMilestoneKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void Reset()
    {
        lock (_sync)
        {
            SaveInternal(StreakPersistenceState.Empty());
        }
    }

    private void SaveInternal(StreakPersistenceState state)
    {
        if (!Directory.Exists(AppDirectory))
        {
            Directory.CreateDirectory(AppDirectory);
        }

        string json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(StateFilePath, json);
    }
}

