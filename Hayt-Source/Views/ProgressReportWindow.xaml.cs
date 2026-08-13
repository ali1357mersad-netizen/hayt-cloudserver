using System;
using System.Windows;
using System.Windows.Controls;
using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.ViewModels;

namespace Hayt.Views
{
    public partial class ProgressReportWindow : Window
    {
        private readonly AppDbContext _dbContext;
        private readonly ProgressReportViewModel _viewModel;
        private readonly ICurrentUserService _currentUserService;

        public ProgressReportWindow(ICurrentUserService currentUserService)
        {
            InitializeComponent();

            _dbContext = new AppDbContext();

            var dataService = new SqliteDataService(_dbContext, currentUserService);
            _currentUserService = currentUserService;

            _viewModel = new ProgressReportViewModel(dataService);

            DataContext = _viewModel;

            Loaded += ProgressReportWindow_Loaded;
            Closed += ProgressReportWindow_Closed;
        }

        private async void ProgressReportWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await _viewModel.LoadAsync();
        }

        private void CertificateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button button ||
                    button.Tag is not BookProgressReport report)
                {
                    MessageBox.Show(
                        this,
                        "اطلاعات کتاب برای صدور گواهی پیدا نشد.",
                        "گواهی پایان کتاب",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (!report.CanGetCertificate ||
                    report.TotalLessons <= 0 ||
                    report.CompletedLessons < report.TotalLessons)
                {
                    MessageBox.Show(
                        this,
                        "برای دریافت گواهی باید تمام درس‌های این کتاب را کامل کنید.",
                        "گواهی هنوز آماده نیست",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var filePath =
                    CertificateService.GenerateAndOpen(report);

                MessageBox.Show(
                    this,
                    "گواهی با موفقیت ساخته و در مرورگر باز شد." +
                    Environment.NewLine +
                    Environment.NewLine +
                    "مسیر فایل:" +
                    Environment.NewLine +
                    filePath +
                    Environment.NewLine +
                    Environment.NewLine +
                    "برای دریافت PDF، در صفحه گواهی روی " +
                    "«چاپ یا ذخیره به‌صورت PDF» کلیک کنید.",
                    "صدور موفق گواهی",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "هنگام ساخت یا باز کردن گواهی خطایی رخ داد:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "خطای صدور گواهی",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void LessonButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button button)
                {
                    return;
                }

                if (button.Tag is not int lessonId || lessonId <= 0)
                {
                    MessageBox.Show(
                        this,
                        "شناسه درس معتبر نیست.",
                        "باز کردن درس",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                OpenLessonReader(lessonId);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "هنگام باز کردن درس خطایی رخ داد:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "خطای باز کردن درس",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ContinueButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button button ||
                    button.Tag is not BookProgressReport report)
                {
                    MessageBox.Show(
                        this,
                        "اطلاعات کتاب برای ادامه آموزش پیدا نشد.",
                        "ادامه آموزش",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (report.RemainingLessonItems.Count == 0)
                {
                    MessageBox.Show(
                        this,
                        "همه درس‌های این کتاب کامل شده است. 🎉",
                        "ادامه آموزش",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var nextLessonId =
                    report.RemainingLessonItems[0].LessonId;

                if (nextLessonId <= 0)
                {
                    MessageBox.Show(
                        this,
                        "شناسه درس بعدی معتبر نیست.",
                        "ادامه آموزش",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                OpenLessonReader(nextLessonId);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "هنگام ادامه آموزش خطایی رخ داد:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "خطای ادامه آموزش",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void OpenLessonReader(int lessonId)
        {
            try
            {
                var lessonWindow = new LessonReaderWindow(lessonId, _currentUserService)
                {
                    Owner = this
                };

                lessonWindow.ShowDialog();

                await _viewModel.LoadAsync();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "هنگام نمایش پنجره درس خطایی رخ داد:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "خطای درس‌خوان",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void ProgressReportWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _dbContext.Dispose();
        }
    }
}

