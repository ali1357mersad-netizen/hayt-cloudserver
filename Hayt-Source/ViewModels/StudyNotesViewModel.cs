using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public enum StudyNotesContextMode
{
    General = 0,
    Book = 1,
    Lesson = 2
}

public partial class StudyNotesViewModel : ObservableObject
{
    private readonly IStudyNotesService _notesService;

    [ObservableProperty]
    private StudyNotesSnapshot _snapshot = StudyNotesSnapshot.Empty;

    [ObservableProperty]
    private StudyNote? _selectedNote;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _importantOnly;

    [ObservableProperty]
    private string _editorTitle = string.Empty;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private string _editorTags = string.Empty;

    [ObservableProperty]
    private bool _editorIsImportant;

    [ObservableProperty]
    private StudyNotesContextMode _contextMode = StudyNotesContextMode.General;

    [ObservableProperty]
    private string? _contextBookId;

    [ObservableProperty]
    private string? _contextBookTitle;

    [ObservableProperty]
    private string? _contextLessonId;

    [ObservableProperty]
    private string? _contextLessonTitle;

    public IReadOnlyList<StudyNote> Notes => Snapshot.Notes;

    public IReadOnlyList<StudyNote> PinnedItems => Snapshot.PinnedItems;

    public IReadOnlyList<StudyNote> RecentItems => Snapshot.RecentItems;

    public int TotalNotes => Snapshot.TotalNotes;

    public int ImportantNotes => Snapshot.ImportantNotes;

    public int PinnedNotes => Snapshot.PinnedNotes;

    public bool HasNotes => Snapshot.HasNotes;

    public bool HasContext => ContextMode != StudyNotesContextMode.General;

    public string ContextTitle => Snapshot.ContextTitle;

    public string SummaryText => Snapshot.ContextSummaryText;

    public string NoteCountText => Snapshot.NoteCountText;

    public string WindowTitleText =>
        $"یادداشت‌ها - {Snapshot.ContextTitle}";

    public string ContextBadgeText
    {
        get
        {
            return ContextMode switch
            {
                StudyNotesContextMode.Book => "حالت کتاب",
                StudyNotesContextMode.Lesson => "حالت درس",
                _ => "حالت عمومی"
            };
        }
    }

    public StudyNotesViewModel(IStudyNotesService notesService)
    {
        _notesService = notesService ??
            throw new ArgumentNullException(nameof(notesService));

        Snapshot = _notesService.Load();

        _notesService.NotesChanged += (_, _) =>
        {
            Snapshot = _notesService.Current;
            RaiseState();
        };

        RaiseState();
    }

    public void SetBookContext(string bookId, string? bookTitle = null)
    {
        ContextMode = StudyNotesContextMode.Book;
        ContextBookId = bookId;
        ContextBookTitle = bookTitle;
        ContextLessonId = null;
        ContextLessonTitle = null;

        RefreshByContext();
    }

    public void SetLessonContext(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        ContextMode = StudyNotesContextMode.Lesson;
        ContextBookId = bookId;
        ContextBookTitle = bookTitle;
        ContextLessonId = lessonId;
        ContextLessonTitle = lessonTitle;

        RefreshByContext();
    }

    public void ClearContext()
    {
        ContextMode = StudyNotesContextMode.General;
        ContextBookId = null;
        ContextBookTitle = null;
        ContextLessonId = null;
        ContextLessonTitle = null;

        RefreshByContext();
    }

    [RelayCommand]
    private void Refresh()
    {
        RefreshByContext();
    }

    [RelayCommand]
    private void Search()
    {
        RefreshByContext();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        ImportantOnly = false;
        RefreshByContext();
    }

    [RelayCommand]
    private void NewNote()
    {
        SelectedNote = null;
        EditorTitle = string.Empty;
        EditorContent = string.Empty;
        EditorTags = string.Empty;
        EditorIsImportant = false;
    }

    [RelayCommand]
    private void LoadSelectedNote()
    {
        if (SelectedNote is null)
        {
            return;
        }

        EditorTitle = SelectedNote.Title;
        EditorContent = SelectedNote.Content;
        EditorTags = SelectedNote.Tags;
        EditorIsImportant = SelectedNote.IsImportant;
    }

    [RelayCommand]
    private void SaveNote()
    {
        if (SelectedNote is null)
        {
            StudyNote note;

            if (ContextMode == StudyNotesContextMode.Lesson && !string.IsNullOrWhiteSpace(ContextLessonId))
            {
                note = _notesService.AddNoteForLesson(
                    ContextBookId,
                    ContextBookTitle,
                    ContextLessonId,
                    ContextLessonTitle,
                    EditorTitle,
                    EditorContent,
                    EditorTags,
                    EditorIsImportant);
            }
            else if (ContextMode == StudyNotesContextMode.Book && !string.IsNullOrWhiteSpace(ContextBookId))
            {
                note = _notesService.AddNoteForBook(
                    ContextBookId,
                    ContextBookTitle,
                    EditorTitle,
                    EditorContent,
                    EditorTags,
                    EditorIsImportant);
            }
            else
            {
                note = _notesService.AddNote(
                    EditorTitle,
                    EditorContent,
                    tags: EditorTags,
                    isImportant: EditorIsImportant);
            }

            SelectedNote = note;
        }
        else
        {
            SelectedNote = _notesService.UpdateNote(
                SelectedNote.Id,
                EditorTitle,
                EditorContent,
                EditorTags,
                EditorIsImportant);
        }

        RefreshByContext();
    }

    [RelayCommand]
    private void DeleteSelectedNote()
    {
        if (SelectedNote is null)
        {
            return;
        }

        _notesService.DeleteNote(SelectedNote.Id);
        NewNote();
        RefreshByContext();
    }

    [RelayCommand]
    private void TogglePinned()
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote = _notesService.TogglePinned(SelectedNote.Id);
        RefreshByContext();
    }

    [RelayCommand]
    private void ToggleImportant()
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote = _notesService.ToggleImportant(SelectedNote.Id);

        if (SelectedNote is not null)
        {
            EditorIsImportant = SelectedNote.IsImportant;
        }

        RefreshByContext();
    }

    [RelayCommand]
    private void ShowAllNotes()
    {
        ClearContext();
    }

    private void RefreshByContext()
    {
        if (ContextMode == StudyNotesContextMode.Lesson && !string.IsNullOrWhiteSpace(ContextLessonId))
        {
            Snapshot = _notesService.LoadForLesson(
                ContextBookId,
                ContextBookTitle,
                ContextLessonId,
                ContextLessonTitle,
                SearchText,
                ImportantOnly);
        }
        else if (ContextMode == StudyNotesContextMode.Book && !string.IsNullOrWhiteSpace(ContextBookId))
        {
            Snapshot = _notesService.LoadForBook(
                ContextBookId,
                ContextBookTitle,
                SearchText,
                ImportantOnly);
        }
        else
        {
            Snapshot = _notesService.Search(SearchText, ImportantOnly);
        }

        RaiseState();
    }

    partial void OnSelectedNoteChanged(StudyNote? value)
    {
        if (value is null)
        {
            return;
        }

        EditorTitle = value.Title;
        EditorContent = value.Content;
        EditorTags = value.Tags;
        EditorIsImportant = value.IsImportant;
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshByContext();
    }

    partial void OnImportantOnlyChanged(bool value)
    {
        RefreshByContext();
    }

    partial void OnSnapshotChanged(StudyNotesSnapshot value)
    {
        RaiseState();
    }

    partial void OnContextModeChanged(StudyNotesContextMode value)
    {
        RaiseState();
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(PinnedItems));
        OnPropertyChanged(nameof(RecentItems));

        OnPropertyChanged(nameof(TotalNotes));
        OnPropertyChanged(nameof(ImportantNotes));
        OnPropertyChanged(nameof(PinnedNotes));
        OnPropertyChanged(nameof(HasNotes));
        OnPropertyChanged(nameof(HasContext));

        OnPropertyChanged(nameof(ContextTitle));
        OnPropertyChanged(nameof(ContextBadgeText));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(NoteCountText));
        OnPropertyChanged(nameof(WindowTitleText));
    }
}

