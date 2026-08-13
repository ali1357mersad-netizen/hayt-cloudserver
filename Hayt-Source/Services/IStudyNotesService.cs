using System;
using System.Collections.Generic;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IStudyNotesService
{
    StudyNotesSnapshot Current { get; }

    event EventHandler? NotesChanged;

    StudyNotesSnapshot Load();

    StudyNotesSnapshot Search(string? query, bool importantOnly = false);

    StudyNotesSnapshot LoadForBook(
        string bookId,
        string? bookTitle = null,
        string? query = null,
        bool importantOnly = false);

    StudyNotesSnapshot LoadForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null,
        string? query = null,
        bool importantOnly = false);

    StudyNote AddNote(
        string title,
        string content,
        string? bookId = null,
        string? bookTitle = null,
        string? lessonId = null,
        string? lessonTitle = null,
        string? tags = null,
        bool isImportant = false);

    StudyNote AddNoteForBook(
        string bookId,
        string? bookTitle,
        string title,
        string content,
        string? tags = null,
        bool isImportant = false);

    StudyNote AddNoteForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle,
        string title,
        string content,
        string? tags = null,
        bool isImportant = false);

    StudyNote? UpdateNote(
        string id,
        string title,
        string content,
        string? tags = null,
        bool? isImportant = null);

    bool DeleteNote(string id);

    StudyNote? TogglePinned(string id);

    StudyNote? ToggleImportant(string id);

    int CountForBook(string bookId);

    int CountForLesson(string lessonId);

    IReadOnlyList<StudyNote> GetAll();
}

