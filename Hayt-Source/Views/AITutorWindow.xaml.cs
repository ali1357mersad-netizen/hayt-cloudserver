using System.Windows;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.ViewModels;

namespace Hayt.Views;

public partial class AITutorWindow : Window
{
    public AITutorWindow()
        : this(new AITutorViewModel(new AITutorService(new StudyNotesService())))
    {
    }

    public AITutorWindow(AITutorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public static AITutorWindow ForGeneral()
    {
        var viewModel = new AITutorViewModel(new AITutorService(new StudyNotesService()));
        viewModel.StartGeneral();
        return new AITutorWindow(viewModel);
    }

    public static AITutorWindow ForBook(string bookId, string? bookTitle = null)
    {
        var viewModel = new AITutorViewModel(new AITutorService(new StudyNotesService()));
        viewModel.StartForBook(bookId, bookTitle);
        return new AITutorWindow(viewModel);
    }

    public static AITutorWindow ForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        var viewModel = new AITutorViewModel(new AITutorService(new StudyNotesService()));
        viewModel.StartForLesson(bookId, bookTitle, lessonId, lessonTitle);
        return new AITutorWindow(viewModel);
    }
}

