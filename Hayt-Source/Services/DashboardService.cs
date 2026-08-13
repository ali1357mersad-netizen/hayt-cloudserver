using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Microsoft.EntityFrameworkCore;

namespace Hayt.Services;

/// <summary>
/// سرویس داشبورد تحلیلی مبتنی بر داده‌های واقعی موجود در پروژه.
/// این نسخه فقط از فیلدهای موجود در مدل‌های فعلی استفاده می‌کند.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = string.IsNullOrWhiteSpace(_currentUserService.CurrentUserId)
            ? "default"
            : _currentUserService.CurrentUserId;

        var now = DateTime.Now;
        var weekStart = now.Date.AddDays(-6);

        // ------------------------------------------------------------
        // داده‌های پیشرفت کاربر
        // ------------------------------------------------------------
        var userProgressList = await _dbContext.UserProgresses
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .ToListAsync(cancellationToken);

        var totalProgressRows = userProgressList.Count;
        var completedRows = userProgressList.Count(x => x.IsCompleted);
        var totalScore = userProgressList.Sum(x => x.Score);
        var totalStudyMinutes = EstimateStudyMinutes(userProgressList);

        var completedLessonIds = userProgressList
            .Where(x => x.IsCompleted)
            .Select(x => x.LessonId)
            .Distinct()
            .ToHashSet();

        // ------------------------------------------------------------
        // درس‌ها و سوال‌ها
        // ------------------------------------------------------------
        var allLessons = await _dbContext.Lessons
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allQuestions = await _dbContext.Questions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalLessons = allLessons.Count;
        var completedLessons = allLessons.Count(x => completedLessonIds.Contains(x.Id));

        var totalQuizzes = allQuestions.Count;
        var passedQuizzes = 0;

        var overallProgressPercent = totalLessons == 0
            ? 0
            : Math.Round((double)completedLessons * 100.0 / totalLessons, 2);

        var quizSuccessPercent = 0.0;

        // ------------------------------------------------------------
        // کتاب‌ها
        // ------------------------------------------------------------
        var allBooks = await _dbContext.Books
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var bookItems = BuildBookProgressItems(
            allBooks,
            allLessons,
            userProgressList,
            completedLessonIds);

        var totalBooks = bookItems.Count;
        var startedBooks = bookItems.Count(x => x.CompletedLessons > 0);
        var completedBooks = bookItems.Count(x => x.IsCompleted);

        // ------------------------------------------------------------
        // فعالیت هفتگی
        // ------------------------------------------------------------
        var weeklyActivity = userProgressList
            .Where(x => x.UpdatedAt.Date >= weekStart)
            .GroupBy(x => x.UpdatedAt.Date)
            .Select(g => new DashboardChartPoint
            {
                Label = g.Key.ToString("MM-dd"),
                Value = g.Sum(x => (double)x.Score),
                Date = g.Key
            })
            .OrderBy(x => x.Date)
            .ToList();

        // ------------------------------------------------------------
        // توزیع پیشرفت
        // ------------------------------------------------------------
        var progressDistribution = new List<DashboardChartPoint>
        {
            new DashboardChartPoint
            {
                Label = "Completed",
                Value = completedLessons
            },
            new DashboardChartPoint
            {
                Label = "In Progress",
                Value = Math.Max(totalLessons - completedLessons, 0)
            }
        };

        // ------------------------------------------------------------
        // عملکرد کوییز
        // ------------------------------------------------------------
        var quizPerformance = new List<DashboardChartPoint>
        {
            new DashboardChartPoint
            {
                Label = "Questions",
                Value = totalQuizzes
            },
            new DashboardChartPoint
            {
                Label = "Passed",
                Value = passedQuizzes
            }
        };

        return new DashboardSnapshot
        {
            Summary = new DashboardSummary
            {
                TotalBooks = totalBooks,
                StartedBooks = startedBooks,
                CompletedBooks = completedBooks,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                TotalQuizzes = totalQuizzes,
                PassedQuizzes = passedQuizzes,
                TotalXp = totalScore,
                CurrentLevel = CalculateLevel(totalScore),
                CurrentStreakDays = CalculateCurrentStreakDays(userProgressList),
                TotalStudyMinutes = totalStudyMinutes,
                OverallProgressPercent = overallProgressPercent,
                QuizSuccessPercent = quizSuccessPercent
            },
            WeeklyActivity = weeklyActivity,
            ProgressDistribution = progressDistribution,
            QuizPerformance = quizPerformance,
            Books = bookItems,
            GeneratedAt = now
        };
    }

    private static int CalculateLevel(int totalScore)
    {
        if (totalScore <= 0)
            return 1;

        return (totalScore / 100) + 1;
    }

    private static int CalculateCurrentStreakDays(List<UserProgress> progressList)
    {
        if (progressList.Count == 0)
            return 0;

        var activeDays = progressList
            .Select(x => x.UpdatedAt.Date)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        if (activeDays.Count == 0)
            return 0;

        int streak = 1;
        var current = activeDays[0];

        for (int i = 1; i < activeDays.Count; i++)
        {
            var previous = activeDays[i];
            if ((current - previous).TotalDays == 1)
            {
                streak++;
                current = previous;
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    private static int EstimateStudyMinutes(List<UserProgress> progressList)
    {
        return progressList.Count * 10;
    }

    private static List<DashboardBookProgressItem> BuildBookProgressItems(
        List<Book> books,
        List<Lesson> lessons,
        List<UserProgress> userProgressList,
        HashSet<int> completedLessonIds)
    {
        var result = new List<DashboardBookProgressItem>();

        foreach (var book in books)
        {
            var bookLessons = lessons
                .Where(l => IsLessonRelatedToBook(l, book))
                .ToList();

            var totalLessons = bookLessons.Count;
            var completedLessons = bookLessons.Count(l => completedLessonIds.Contains(l.Id));

            var progressPercent = totalLessons == 0
                ? 0
                : Math.Round((double)completedLessons * 100.0 / totalLessons, 2);

            var earnedXp = bookLessons
                .Join(
                    userProgressList,
                    lesson => lesson.Id,
                    progress => progress.LessonId,
                    (lesson, progress) => progress.Score)
                .Sum();

            var lastActivityAt = userProgressList
                .Where(p => bookLessons.Any(l => l.Id == p.LessonId))
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => (DateTime?)p.UpdatedAt)
                .FirstOrDefault();

            result.Add(new DashboardBookProgressItem
            {
                BookId = GetBookIdentifier(book),
                Title = GetBookTitle(book),
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                ProgressPercent = progressPercent,
                EarnedXp = earnedXp,
                IsCompleted = totalLessons > 0 && completedLessons >= totalLessons,
                LastActivityAt = lastActivityAt
            });
        }

        return result;
    }

    private static bool IsLessonRelatedToBook(Lesson lesson, Book book)
    {
        var lessonType = lesson.GetType();

        var chapterProp = lessonType.GetProperty("Chapter");
        var chapter = chapterProp?.GetValue(lesson);
        if (chapter != null)
        {
            var chapterType = chapter.GetType();

            var sectionProp = chapterType.GetProperty("Section");
            var section = sectionProp?.GetValue(chapter);
            if (section != null)
            {
                var sectionType = section.GetType();

                var bookProp = sectionType.GetProperty("Book");
                var linkedBook = bookProp?.GetValue(section);
                if (linkedBook != null)
                {
                    return ReferenceEquals(linkedBook, book);
                }

                var bookIdProp = sectionType.GetProperty("BookId");
                var bookId = bookIdProp?.GetValue(section);
                var bookIdValue = GetBookIdAsObject(book);
                if (bookId != null && bookIdValue != null && bookId.Equals(bookIdValue))
                {
                    return true;
                }
            }

            var chapterBookProp = chapterType.GetProperty("Book");
            var chapterBook = chapterBookProp?.GetValue(chapter);
            if (chapterBook != null)
            {
                return ReferenceEquals(chapterBook, book);
            }

            var chapterBookIdProp = chapterType.GetProperty("BookId");
            var chapterBookId = chapterBookIdProp?.GetValue(chapter);
            var bookId2 = GetBookIdAsObject(book);
            if (chapterBookId != null && bookId2 != null && chapterBookId.Equals(bookId2))
            {
                return true;
            }
        }

        var lessonBookIdProp = lessonType.GetProperty("BookId");
        if (lessonBookIdProp != null)
        {
            var lessonBookId = lessonBookIdProp.GetValue(lesson);
            var bookId = GetBookIdAsObject(book);
            if (lessonBookId != null && bookId != null && lessonBookId.Equals(bookId))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetBookIdentifier(Book book)
    {
        var type = book.GetType();

        foreach (var propName in new[] { "Id", "BookId", "Code", "Key" })
        {
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                var value = prop.GetValue(book);
                if (value != null)
                    return value.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static object? GetBookIdAsObject(Book book)
    {
        var type = book.GetType();

        foreach (var propName in new[] { "Id", "BookId" })
        {
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
                return prop.GetValue(book);
        }

        return null;
    }

    private static string GetBookTitle(Book book)
    {
        var type = book.GetType();

        foreach (var propName in new[] { "Title", "Name", "BookTitle" })
        {
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                var value = prop.GetValue(book);
                if (value != null)
                    return value.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}


