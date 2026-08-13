using System.Windows;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.ViewModels;

namespace Hayt.Views;

public partial class StudyNotesWindow : Window
{
    public StudyNotesWindow()
        : this(new StudyNotesViewModel(new StudyNotesService()))
    {
    }

    public StudyNotesWindow(StudyNotesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public StudyNotesWindow(string bookId, string? bookTitle = null)
        : this(CreateForBook(bookId, bookTitle))
    {
    }

    public StudyNotesWindow(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
        : this(CreateForLesson(bookId, bookTitle, lessonId, lessonTitle))
    {
    }

    public static StudyNotesWindow ForBook(
        string bookId,
        string? bookTitle = null)
    {
        return new StudyNotesWindow(CreateForBook(bookId, bookTitle));
    }

    public static StudyNotesWindow ForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        return new StudyNotesWindow(CreateForLesson(bookId, bookTitle, lessonId, lessonTitle));
    }

    private static StudyNotesViewModel CreateForBook(
        string bookId,
        string? bookTitle)
    {
        var viewModel = new StudyNotesViewModel(new StudyNotesService());
        viewModel.SetBookContext(bookId, bookTitle);
        return viewModel;
    }

    private static StudyNotesViewModel CreateForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle)
    {
        var viewModel = new StudyNotesViewModel(new StudyNotesService());
        viewModel.SetLessonContext(bookId, bookTitle, lessonId, lessonTitle);
        return viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

