using System.Collections.Generic;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IStreakPersistenceService
{
    StreakPersistenceState Load();

    void Save(StreakPersistenceState state);

    void SaveFromSnapshot(StreakSnapshot snapshot, IEnumerable<string> notifiedMilestoneKeys);

    bool IsMilestoneNotified(string key);

    void MarkMilestoneNotified(string key);

    IReadOnlyCollection<string> GetNotifiedMilestoneKeys();

    void Reset();
}

