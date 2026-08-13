using System;

namespace Hayt.Models;

/// <summary>
/// Milestone مربوط به زنجیره یادگیری.
/// در مرحله 16-B3 فقط Runtime است و در دیتابیس ذخیره نمی‌شود.
/// </summary>
public sealed class StreakMilestone
{
    public string Key { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = "🔥";

    public int RequiredDays { get; init; }

    public bool IsUnlocked { get; init; }

    public DateTime? UnlockedAt { get; init; }

    public int RemainingDays
    {
        get
        {
            if (IsUnlocked)
            {
                return 0;
            }

            return Math.Max(RequiredDays, 0);
        }
    }

    public string StatusText =>
        IsUnlocked
            ? "آزاد شد"
            : $"{RequiredDays} روز";

    public string RequiredText =>
        $"{RequiredDays} روز";
}