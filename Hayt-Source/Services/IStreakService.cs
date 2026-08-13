using System;
using System.Collections.Generic;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IStreakService
{
    StreakSnapshot Current { get; }

    event EventHandler? StreakChanged;

    IReadOnlyList<StreakMilestone> MilestoneDefinitions { get; }

    StreakSnapshot Calculate(
        IEnumerable<DateTime> activityDates,
        DateTime? referenceDate = null);

    StreakSnapshot Evaluate(
        DashboardSnapshot? dashboardSnapshot,
        DateTime? referenceDate = null);

    void ResetRuntimeState();
}

