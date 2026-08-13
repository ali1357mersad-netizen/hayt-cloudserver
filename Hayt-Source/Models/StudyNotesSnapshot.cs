using System;
using System.Collections.Generic;
using System.Linq;

namespace Hayt.Models;

public sealed class StudyNotesSnapshot
{
    public static StudyNotesSnapshot Empty { get; } = new();

    public DateTime EvaluatedAt { get; init; } = DateTime.Now;

    public IReadOnlyList<StudyNote> Notes { get; init; } =
        Array.Empty<StudyNote>();

    public string SearchText { get; init; } = string.Empty;

    public bool ImportantOnly { get; init; }

    public string? BookId { get; init; }

    public string? BookTitle { get; init; }

    public string? LessonId { get; init; }

    public string? LessonTitle { get; init; }

    public bool HasBookContext =>
        !string.IsNullOrWhiteSpace(BookId) ||
        !string.IsNullOrWhiteSpace(BookTitle);

    public bool HasLessonContext =>
        !string.IsNullOrWhiteSpace(LessonId) ||
        !string.IsNullOrWhiteSpace(LessonTitle);

    public bool HasContext =>
        HasBookContext || HasLessonContext;

    public int TotalNotes =>
        Notes.Count;

    public int ImportantNotes =>
        Notes.Count(x => x.IsImportant);

    public int PinnedNotes =>
        Notes.Count(x => x.IsPinned);

    public int BookNotes =>
        string.IsNullOrWhiteSpace(BookId)
            ? 0
            : Notes.Count(x => x.BelongsToBook(BookId));

    public int LessonNotes =>
        string.IsNullOrWhiteSpace(LessonId)
            ? 0
            : Notes.Count(x => x.BelongsToLesson(LessonId));

    public bool HasNotes =>
        TotalNotes > 0;

    public bool HasPinnedNotes =>
        PinnedNotes > 0;

    public bool HasImportantNotes =>
        ImportantNotes > 0;

    public IReadOnlyList<StudyNote> PinnedItems =>
        Notes
            .Where(x => x.IsPinned)
            .OrderByDescending(x => x.UpdatedAt)
            .ToArray();

    public IReadOnlyList<StudyNote> RecentItems =>
        Notes
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.UpdatedAt)
            .Take(10)
            .ToArray();

    public string ContextTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(BookTitle) && !string.IsNullOrWhiteSpace(LessonTitle))
            {
                return $"یادداشت‌های درس: {BookTitle} / {LessonTitle}";
            }

            if (!string.IsNullOrWhiteSpace(LessonTitle))
            {
                return $"یادداشت‌های درس: {LessonTitle}";
            }

            if (!string.IsNullOrWhiteSpace(BookTitle))
            {
                return $"یادداشت‌های کتاب: {BookTitle}";
            }

            if (!string.IsNullOrWhiteSpace(BookId) && !string.IsNullOrWhiteSpace(LessonId))
            {
                return $"یادداشت‌های Book:{BookId} / Lesson:{LessonId}";
            }

            if (!string.IsNullOrWhiteSpace(BookId))
            {
                return $"یادداشت‌های کتاب: {BookId}";
            }

            if (!string.IsNullOrWhiteSpace(LessonId))
            {
                return $"یادداشت‌های درس: {LessonId}";
            }

            return "همه یادداشت‌ها";
        }
    }

    public string SummaryText =>
        TotalNotes == 0
            ? "هنوز یادداشتی ثبت نشده است"
            : $"{TotalNotes} یادداشت، {ImportantNotes} مهم، {PinnedNotes} سنجاق‌شده";

    public string ContextSummaryText =>
        HasContext
            ? $"{ContextTitle} • {SummaryText}"
            : SummaryText;

    public string NoteCountText =>
        TotalNotes == 0
            ? "بدون یادداشت"
            : TotalNotes == 1
                ? "۱ یادداشت"
                : $"{TotalNotes} یادداشت";
}