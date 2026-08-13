using System;
using System.Reflection;
using Hayt.Services;
using Hayt.Licensing.Services;
using System.Windows;
using Hayt.ViewModels;

namespace Hayt.Views;

public partial class DashboardWindow : Window
{
    public DashboardWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += DashboardWindow_Loaded;
    }

    private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= DashboardWindow_Loaded;

        if (DataContext is DashboardViewModel vm)
        {
            await vm.LoadAsync();
            await vm.CloudSyncVM.InitializeAsync();
        }
    }

    private void OpenPersonalGoalsWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            PersonalGoalsViewModel goalsViewModel = ResolvePersonalGoalsViewModel();

            var window = new PersonalGoalsWindow(goalsViewModel)
            {
                Owner = this
            };

            window.ShowDialog();

            goalsViewModel.RefreshStateCommand.Execute(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "باز کردن پنجره مدیریت اهداف با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "مدیریت اهداف شخصی",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private PersonalGoalsViewModel ResolvePersonalGoalsViewModel()
    {
        if (DataContext is PersonalGoalsViewModel directGoalsViewModel)
        {
            return directGoalsViewModel;
        }

        if (DataContext is not null)
        {
            PropertyInfo? goalsProperty = DataContext
                .GetType()
                .GetProperty("GoalsVM", BindingFlags.Instance | BindingFlags.Public);

            if (goalsProperty?.GetValue(DataContext) is PersonalGoalsViewModel goalsViewModel)
            {
                return goalsViewModel;
            }

            PropertyInfo? personalGoalsProperty = DataContext
                .GetType()
                .GetProperty("PersonalGoalsVM", BindingFlags.Instance | BindingFlags.Public);

            if (personalGoalsProperty?.GetValue(DataContext) is PersonalGoalsViewModel personalGoalsViewModel)
            {
                return personalGoalsViewModel;
            }
        }

        return new PersonalGoalsViewModel(new PersonalGoalsService());
    }

    private void OpenStudyNotesWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new StudyNotesWindow(new StudyNotesViewModel(new StudyNotesService()))
            {
                Owner = this
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "باز کردن پنجره یادداشت‌ها با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "یادداشت‌ها",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void OpenStudyNotesWindowForBook(string bookId, string? bookTitle = null)
    {
        try
        {
            var window = new StudyNotesWindow(bookId, bookTitle)
            {
                Owner = this
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "باز کردن یادداشت‌های کتاب با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "یادداشت‌های کتاب",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void OpenStudyNotesWindowForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        try
        {
            var window = new StudyNotesWindow(bookId, bookTitle, lessonId, lessonTitle)
            {
                Owner = this
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "باز کردن یادداشت‌های درس با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "یادداشت‌های درس",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void OpenAITutorWindow()
    {
        try
        {
            var window = AITutorWindow.ForGeneral();
            window.Owner = this;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "باز کردن مربی هوشمند با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "مربی هوشمند",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void OpenAITutorWindowForBook(string bookId, string? bookTitle = null)
    {
        try
        {
            var window = AITutorWindow.ForBook(bookId, bookTitle);
            window.Owner = this;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "باز کردن مربی هوشمند کتاب با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "مربی هوشمند",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void OpenAITutorWindowForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        try
        {
            var window = AITutorWindow.ForLesson(bookId, bookTitle, lessonId, lessonTitle);
            window.Owner = this;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "باز کردن مربی هوشمند درس با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "مربی هوشمند",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OpenAISettingsWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new AISettingsWindow
            {
                Owner = this
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "باز کردن تنظیمات هوش مصنوعی با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "تنظیمات AI",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenLicenseWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new LicenseWindow
            {
                Owner = this
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "باز کردن پنجره لایسنس با خطا مواجه شد:" + Environment.NewLine + ex.Message,
                "لایسنس",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}


