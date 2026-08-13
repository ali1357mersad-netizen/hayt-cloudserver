using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Hayt.Models;

public partial class UserAchievementState : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = "🏅";

    [ObservableProperty]
    private bool _isUnlocked;

    [ObservableProperty]
    private DateTime? _unlockedAt;

    [ObservableProperty]
    private string _progressText = "قفل است";

    [ObservableProperty]
    private int _sortOrder;
}