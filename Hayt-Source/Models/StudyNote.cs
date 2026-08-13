using System;

namespace Hayt.Models;

public sealed class StudyNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? BookId { get; set; }

    public string? BookTitle { get; set; }

    public string? LessonId { get; set; }

    public string? LessonTitle { get; set; }

    public string Tags { get; set; } = string.Empty;

    public bool IsPinned { get; set; }

    public bool IsImportant { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool HasBookContext =>
        !string.IsNullOrWhiteSpace(BookId) ||
        !string.IsNullOrWhiteSpace(BookTitle);

    public bool HasLessonContext =>
        !string.IsNullOrWhiteSpace(LessonId) ||
        !string.IsNullOrWhiteSpace(LessonTitle);

    public string DisplayTitle =>
        string.IsNullOrWhiteSpace(Title)
            ? "یادداشت بدون عنوان"
            : Title.Trim();

    public string ShortContent
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                return "بدون متن";
            }

            string text = Content.Replace("\r", " ").Replace("\n", " ").Trim();

            return text.Length <= 120
                ? text
                : text[..120] + "...";
        }
    }

    public string LocationText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(BookTitle) && !string.IsNullOrWhiteSpace(LessonTitle))
            {
                return $"{BookTitle} / {LessonTitle}";
            }

            if (!string.IsNullOrWhiteSpace(BookTitle))
            {
                return BookTitle!;
            }

            if (!string.IsNullOrWhiteSpace(LessonTitle))
            {
                return LessonTitle!;
            }

            return "عمومی";
        }
    }

    public string ContextKeyText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(BookId) && !string.IsNullOrWhiteSpace(LessonId))
            {
                return $"Book:{BookId} | Lesson:{LessonId}";
            }

            if (!string.IsNullOrWhiteSpace(BookId))
            {
                return $"Book:{BookId}";
            }

            if (!string.IsNullOrWhiteSpace(LessonId))
            {
                return $"Lesson:{LessonId}";
            }

            return "General";
        }
    }

    public string FlagsText
    {
        get
        {
            if (IsPinned && IsImportant)
            {
                return "سنجاق‌شده • مهم";
            }

            if (IsPinned)
            {
                return "سنجاق‌شده";
            }

            if (IsImportant)
            {
                return "مهم";
            }

            return "معمولی";
        }
    }

    public string UpdatedText =>
        $"آخرین ویرایش: {UpdatedAt:yyyy/MM/dd HH:mm}";

    public bool Matches(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        string q = query.Trim();

        return Contains(Title, q) ||
               Contains(Content, q) ||
               Contains(BookTitle, q) ||
               Contains(LessonTitle, q) ||
               Contains(BookId, q) ||
               Contains(LessonId, q) ||
               Contains(Tags, q);
    }

    public bool BelongsToBook(string? bookId)
    {
        if (string.IsNullOrWhiteSpace(bookId))
        {
            return true;
        }

        return string.Equals(BookId, bookId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool BelongsToLesson(string? lessonId)
    {
        if (string.IsNullOrWhiteSpace(lessonId))
        {
            return true;
        }

        return string.Equals(LessonId, lessonId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool BelongsToBookAndLesson(string? bookId, string? lessonId)
    {
        return BelongsToBook(bookId) && BelongsToLesson(lessonId);
    }

    private static bool Contains(string? source, string value)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}