using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IAchievementService
{
    ObservableCollection<UserAchievementState> Achievements { get; }

    event EventHandler? AchievementsChanged;

    Task EvaluateAsync(DashboardSnapshot? snapshot);

    void ResetRuntimeState();
}

