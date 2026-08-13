using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Hayt.Views
{
    public partial class UserProfileWindow : Window
    {
        private readonly ICurrentUserService _currentUserService;

        public UserProfileWindow(ICurrentUserService currentUserService)
        {
            InitializeComponent();

            _currentUserService = currentUserService ??
                throw new ArgumentNullException(nameof(currentUserService));

            Loaded += async (_, _) => await LoadProfileAsync();
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                StatusText.Text = "در حال بارگذاری پروفایل...";

                var user = _currentUserService.CurrentUser;

                UserNameText.Text =
                    string.IsNullOrWhiteSpace(user?.DisplayName)
                        ? "کاربر اصلی"
                        : user.DisplayName;

                UserIdText.Text =
                    "شناسه: " + ToPersianNumber(_currentUserService.CurrentUserId);

                using var db = new AppDbContext();

                IDataService dataService =
                    new SqliteDataService(db, _currentUserService);

                var totalLessons = await dataService.GetTotalLessonCountAsync();
                var completedLessons = await dataService.GetCompletedLessonCountAsync();
                var totalScore = await dataService.GetTotalScoreAsync();
                var reports = await dataService.GetBookProgressReportsAsync();

                var completedBooks = reports.Count(x => x.IsCompleted);
                var certificates = completedBooks;
                var totalHours = reports.Sum(x => x.CompletedHours);

                var percent = totalLessons <= 0
                    ? 0
                    : completedLessons * 100.0 / totalLessons;

                var level = BookProgressReport.GetLevelTitle(totalScore);

                OverallPercentText.Text =
                    ToPersianNumber(percent.ToString("0.#", CultureInfo.InvariantCulture)) + "٪";

                LessonsText.Text =
                    ToPersianNumber(completedLessons) + " از " + ToPersianNumber(totalLessons);

                TotalScoreText.Text =
                    ToPersianNumber(totalScore.ToString("N0", CultureInfo.InvariantCulture));

                LevelText.Text = level;

                CompletedBooksText.Text =
                    "کتاب‌های کامل‌شده: " + ToPersianNumber(completedBooks);

                CertificatesText.Text =
                    "گواهی‌های آماده: " + ToPersianNumber(certificates);

                HoursText.Text =
                    "ساعت آموزش: " +
                    ToPersianNumber(totalHours.ToString("0.#", CultureInfo.InvariantCulture));

                BooksList.ItemsSource = reports;

                MotivationText.Text = BuildMotivation(
                    completedLessons,
                    totalLessons,
                    completedBooks,
                    totalScore);

                StatusText.Text =
                    "پروفایل با موفقیت به‌روزرسانی شد.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "خطا در بارگذاری پروفایل.";

                MessageBox.Show(
                    "خطا در بارگذاری پروفایل:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "پروفایل کاربری",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string BuildMotivation(
            int completedLessons,
            int totalLessons,
            int completedBooks,
            int totalScore)
        {
            if (totalLessons <= 0)
            {
                return "هنوز محتوایی برای آموزش ثبت نشده است.";
            }

            if (completedLessons <= 0)
            {
                return "اولین درس را شروع کن؛ مسیر پیشرفت از همین‌جا آغاز می‌شود.";
            }

            if (completedLessons >= totalLessons)
            {
                return "تبریک! همه درس‌ها تکمیل شده‌اند. حالا زمان دریافت گواهی‌هاست.";
            }

            if (completedBooks > 0)
            {
                return "عالی پیش رفتی! تو وارد مرحله حرفه‌ای یادگیری شده‌ای.";
            }

            if (totalScore >= 3000)
            {
                return "سطح امتیازت عالی است؛ با ادامه مسیر به سطح مدرس نزدیک می‌شوی.";
            }

            return "ادامه بده؛ هر درس یک قدم به گواهی، مهارت و موفقیت نزدیک‌ترت می‌کند.";
        }

        private async void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadProfileAsync();
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private static string ToPersianNumber(object? value)
        {
            var text = Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ?? string.Empty;

            return text
                .Replace('0', '۰')
                .Replace('1', '۱')
                .Replace('2', '۲')
                .Replace('3', '۳')
                .Replace('4', '۴')
                .Replace('5', '۵')
                .Replace('6', '۶')
                .Replace('7', '۷')
                .Replace('8', '۸')
                .Replace('9', '۹')
                .Replace('.', '٫');
        }
    }
}

