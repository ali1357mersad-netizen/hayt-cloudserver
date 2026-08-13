using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class StudyNotesService : IStudyNotesService
{
    private readonly object _sync = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public StudyNotesSnapshot Current { get; private set; } =
        StudyNotesSnapshot.Empty;

    public event EventHandler? NotesChanged;

    private string AppDirectory
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Hayt");
        }
    }

    private string StateFilePath =>
        Path.Combine(AppDirectory, "study-notes.json");

    public StudyNotesSnapshot Load()
    {
        lock (_sync)
        {
            var state = LoadState();
            Current = BuildSnapshot(
                state.Notes,
                string.Empty,
                false,
                null,
                null,
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return Current;
        }
    }

    public StudyNotesSnapshot Search(string? query, bool importantOnly = false)
    {
        lock (_sync)
        {
            var state = LoadState();

            IEnumerable<StudyNote> notes = ApplyQueryAndImportance(
                state.Notes,
                query,
                importantOnly);

            Current = BuildSnapshot(
                notes,
                query ?? string.Empty,
                importantOnly,
                null,
                null,
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return Current;
        }
    }

    public StudyNotesSnapshot LoadForBook(
        string bookId,
        string? bookTitle = null,
        string? query = null,
        bool importantOnly = false)
    {
        lock (_sync)
        {
            var state = LoadState();

            IEnumerable<StudyNote> notes = state.Notes
                .Where(x => x.BelongsToBook(bookId));

            notes = ApplyQueryAndImportance(notes, query, importantOnly);

            Current = BuildSnapshot(
                notes,
                query ?? string.Empty,
                importantOnly,
                CleanNullable(bookId),
                CleanNullable(bookTitle),
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return Current;
        }
    }

    public StudyNotesSnapshot LoadForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null,
        string? query = null,
        bool importantOnly = false)
    {
        lock (_sync)
        {
            var state = LoadState();

            IEnumerable<StudyNote> notes = state.Notes
                .Where(x => x.BelongsToBookAndLesson(bookId, lessonId));

            notes = ApplyQueryAndImportance(notes, query, importantOnly);

            Current = BuildSnapshot(
                notes,
                query ?? string.Empty,
                importantOnly,
                CleanNullable(bookId),
                CleanNullable(bookTitle),
                CleanNullable(lessonId),
                CleanNullable(lessonTitle));

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return Current;
        }
    }

    public StudyNote AddNote(
        string title,
        string content,
        string? bookId = null,
        string? bookTitle = null,
        string? lessonId = null,
        string? lessonTitle = null,
        string? tags = null,
        bool isImportant = false)
    {
        lock (_sync)
        {
            var state = LoadState();

            var note = new StudyNote
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = Clean(title),
                Content = content?.Trim() ?? string.Empty,
                BookId = CleanNullable(bookId),
                BookTitle = CleanNullable(bookTitle),
                LessonId = CleanNullable(lessonId),
                LessonTitle = CleanNullable(lessonTitle),
                Tags = Clean(tags),
                IsImportant = isImportant,
                IsPinned = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(note.Title) && string.IsNullOrWhiteSpace(note.Content))
            {
                note.Title = "یادداشت جدید";
            }

            state.Notes.Add(note);
            SaveState(state);

            Current = BuildSnapshot(
                state.Notes,
                string.Empty,
                false,
                null,
                null,
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return note;
        }
    }

    public StudyNote AddNoteForBook(
        string bookId,
        string? bookTitle,
        string title,
        string content,
        string? tags = null,
        bool isImportant = false)
    {
        return AddNote(
            title,
            content,
            bookId,
            bookTitle,
            null,
            null,
            tags,
            isImportant);
    }

    public StudyNote AddNoteForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle,
        string title,
        string content,
        string? tags = null,
        bool isImportant = false)
    {
        return AddNote(
            title,
            content,
            bookId,
            bookTitle,
            lessonId,
            lessonTitle,
            tags,
            isImportant);
    }

    public StudyNote? UpdateNote(
        string id,
        string title,
        string content,
        string? tags = null,
        bool? isImportant = null)
    {
        lock (_sync)
        {
            var state = LoadState();
            var note = FindNote(state, id);

            if (note is null)
            {
                return null;
            }

            note.Title = Clean(title);
            note.Content = content?.Trim() ?? string.Empty;
            note.Tags = Clean(tags);

            if (isImportant.HasValue)
            {
                note.IsImportant = isImportant.Value;
            }

            note.UpdatedAt = DateTime.Now;

            SaveState(state);

            Current = BuildSnapshot(
                state.Notes,
                string.Empty,
                false,
                null,
                null,
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return note;
        }
    }

    public bool DeleteNote(string id)
    {
        lock (_sync)
        {
            var state = LoadState();
            var note = FindNote(state, id);

            if (note is null)
            {
                return false;
            }

            state.Notes.Remove(note);
            SaveState(state);

            Current = BuildSnapshot(
                state.Notes,
                string.Empty,
                false,
                null,
                null,
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }

    public StudyNote? TogglePinned(string id)
    {
        lock (_sync)
        {
            var state = LoadState();
            var note = FindNote(state, id);

            if (note is null)
            {
                return null;
            }

            note.IsPinned = !note.IsPinned;
            note.UpdatedAt = DateTime.Now;

            SaveState(state);

            Current = BuildSnapshot(
                state.Notes,
                string.Empty,
                false,
                null,
                null,
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return note;
        }
    }

    public StudyNote? ToggleImportant(string id)
    {
        lock (_sync)
        {
            var state = LoadState();
            var note = FindNote(state, id);

            if (note is null)
            {
                return null;
            }

            note.IsImportant = !note.IsImportant;
            note.UpdatedAt = DateTime.Now;

            SaveState(state);

            Current = BuildSnapshot(
                state.Notes,
                string.Empty,
                false,
                null,
                null,
                null,
                null);

            NotesChanged?.Invoke(this, EventArgs.Empty);
            return note;
        }
    }

    public int CountForBook(string bookId)
    {
        if (string.IsNullOrWhiteSpace(bookId))
        {
            return 0;
        }

        return LoadState()
            .Notes
            .Count(x => x.BelongsToBook(bookId));
    }

    public int CountForLesson(string lessonId)
    {
        if (string.IsNullOrWhiteSpace(lessonId))
        {
            return 0;
        }

        return LoadState()
            .Notes
            .Count(x => x.BelongsToLesson(lessonId));
    }

    public IReadOnlyList<StudyNote> GetAll()
    {
        return LoadState()
            .Notes
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.UpdatedAt)
            .ToArray();
    }

    private static IEnumerable<StudyNote> ApplyQueryAndImportance(
        IEnumerable<StudyNote> notes,
        string? query,
        bool importantOnly)
    {
        if (importantOnly)
        {
            notes = notes.Where(x => x.IsImportant);
        }

        return notes.Where(x => x.Matches(query));
    }

    private StudyNotesSnapshot BuildSnapshot(
        IEnumerable<StudyNote> notes,
        string searchText,
        bool importantOnly,
        string? bookId,
        string? bookTitle,
        string? lessonId,
        string? lessonTitle)
    {
        return new StudyNotesSnapshot
        {
            EvaluatedAt = DateTime.Now,
            SearchText = searchText,
            ImportantOnly = importantOnly,
            BookId = bookId,
            BookTitle = bookTitle,
            LessonId = lessonId,
            LessonTitle = lessonTitle,
            Notes = notes
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.UpdatedAt)
                .ToArray()
        };
    }

    private StudyNotesState LoadState()
    {
        try
        {
            if (!Directory.Exists(AppDirectory))
            {
                Directory.CreateDirectory(AppDirectory);
            }

            if (!File.Exists(StateFilePath))
            {
                var empty = StudyNotesState.Empty();
                SaveState(empty);
                return empty;
            }

            string json = File.ReadAllText(StateFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return StudyNotesState.Empty();
            }

            var state = JsonSerializer.Deserialize<StudyNotesState>(json, _jsonOptions);

            return state ?? StudyNotesState.Empty();
        }
        catch
        {
            return StudyNotesState.Empty();
        }
    }

    private void SaveState(StudyNotesState state)
    {
        state.LastSavedAt = DateTime.Now;

        if (!Directory.Exists(AppDirectory))
        {
            Directory.CreateDirectory(AppDirectory);
        }

        string json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(StateFilePath, json);
    }

    private static StudyNote? FindNote(StudyNotesState state, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return state.Notes.FirstOrDefault(
            x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static string Clean(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? CleanNullable(string? value)
    {
        string cleaned = Clean(value);
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}

