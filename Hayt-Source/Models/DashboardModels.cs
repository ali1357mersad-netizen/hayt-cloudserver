using System;
using System.Collections.Generic;

namespace Hayt.Models;

/// <summary>
/// آمار خلاصه داشبورد برای کاربر جاری.
/// این مدل فقط داده‌های نمایشی را نگه می‌دارد و به دیتابیس وابسته نیست.
/// </summary>
public sealed class DashboardSummary
{
    public int TotalBooks { get; init; }

    public int StartedBooks { get; init; }

    public int CompletedBooks { get; init; }

    public int TotalLessons { get; init; }

    public int CompletedLessons { get; init; }

    public int TotalQuizzes { get; init; }

    public int PassedQuizzes { get; init; }

    public int TotalXp { get; init; }

    public int CurrentLevel { get; init; }

    public int CurrentStreakDays { get; init; }

    public int TotalStudyMinutes { get; init; }

    public double OverallProgressPercent { get; init; }

    public double QuizSuccessPercent { get; init; }
}

/// <summary>
/// یک نقطه عمومی برای نمودارهای ستونی، خطی یا دایره‌ای.
/// </summary>
public sealed class DashboardChartPoint
{
    public string Label { get; init; } = string.Empty;

    public double Value { get; init; }

    public string? ColorHex { get; init; }

    public DateTime? Date { get; init; }
}

/// <summary>
/// وضعیت پیشرفت یک کتاب در داشبورد.
/// </summary>
public sealed class DashboardBookProgressItem
{
    public string BookId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public int TotalLessons { get; init; }

    public int CompletedLessons { get; init; }

    public double ProgressPercent { get; init; }

    public int EarnedXp { get; init; }

    public bool IsCompleted { get; init; }

    public DateTime? LastActivityAt { get; init; }
}

/// <summary>
/// بسته کامل داده‌های داشبورد.
/// در مراحل بعد توسط DashboardService برای کاربر جاری تولید می‌شود.
/// </summary>
public sealed class DashboardSnapshot
{
    public DashboardSummary Summary { get; init; } = new();

    public IReadOnlyList<DashboardChartPoint> WeeklyActivity { get; init; } =
        Array.Empty<DashboardChartPoint>();

    public IReadOnlyList<DashboardChartPoint> ProgressDistribution { get; init; } =
        Array.Empty<DashboardChartPoint>();

    public IReadOnlyList<DashboardChartPoint> QuizPerformance { get; init; } =
        Array.Empty<DashboardChartPoint>();

    public IReadOnlyList<DashboardBookProgressItem> Books { get; init; } =
        Array.Empty<DashboardBookProgressItem>();

    public DateTime GeneratedAt { get; init; } = DateTime.Now;
}