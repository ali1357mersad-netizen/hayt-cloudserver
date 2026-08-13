using System;
using System.Collections.Generic;
using System.Linq;

namespace Hayt.Models;

public sealed class AITutorSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string? BookId { get; set; }

    public string? BookTitle { get; set; }

    public string? LessonId { get; set; }

    public string? LessonTitle { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.Now;

    public DateTime LastActivityAt { get; set; } = DateTime.Now;

    public List<AITutorMessage> Messages { get; set; } = new();

    public bool HasContext =>
        !string.IsNullOrWhiteSpace(BookId) ||
        !string.IsNullOrWhiteSpace(BookTitle) ||
        !string.IsNullOrWhiteSpace(LessonId) ||
        !string.IsNullOrWhiteSpace(LessonTitle);

    public string ContextTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(BookTitle) && !string.IsNullOrWhiteSpace(LessonTitle))
            {
                return $"{BookTitle} / {LessonTitle}";
            }

            if (!string.IsNullOrWhiteSpace(LessonTitle))
            {
                return LessonTitle!;
            }

            if (!string.IsNullOrWhiteSpace(BookTitle))
            {
                return BookTitle!;
            }

            return "گفتگوی عمومی";
        }
    }

    public int MessageCount => Messages.Count;

    public string LastActivityText =>
        LastActivityAt.ToString("yyyy/MM/dd HH:mm");

    public static AITutorSession CreateGeneral()
    {
        return new AITutorSession();
    }

    public static AITutorSession CreateForBook(string bookId, string? bookTitle = null)
    {
        return new AITutorSession
        {
            BookId = bookId,
            BookTitle = bookTitle
        };
    }

    public static AITutorSession CreateForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        return new AITutorSession
        {
            BookId = bookId,
            BookTitle = bookTitle,
            LessonId = lessonId,
            LessonTitle = lessonTitle
        };
    }

    public IReadOnlyList<AITutorMessage> RecentMessages(int count = 20)
    {
        return Messages
            .OrderByDescending(x => x.SentAt)
            .Take(count)
            .OrderBy(x => x.SentAt)
            .ToArray();
    }
}