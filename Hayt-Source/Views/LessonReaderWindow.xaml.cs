using System;
using System.Threading.Tasks;
using System.Windows;
using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.Views
{
    public partial class LessonReaderWindow : Window
    {
        private readonly int _lessonId;
        private readonly AppDbContext _dbContext;
        private readonly SqliteDataService _dataService;
        private readonly ICurrentUserService _currentUserService;

        private Lesson? _lesson;

        public LessonReaderWindow(int lessonId, ICurrentUserService currentUserService)
        {
            InitializeComponent();

            if (lessonId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lessonId),
                    "شناسه درس معتبر نیست.");
            }

            _lessonId = lessonId;
            _dbContext = new AppDbContext();
            _dataService = new SqliteDataService(_dbContext, currentUserService);
            _currentUserService = currentUserService;

            Loaded += LessonReaderWindow_Loaded;
            Closed += LessonReaderWindow_Closed;
        }

        private async void LessonReaderWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await LoadLessonAsync();
        }

        private async Task LoadLessonAsync()
        {
            try
            {
                SetButtonsEnabled(false);

                LessonTitleText.Text = "در حال بارگذاری درس...";
                LessonContentText.Text = "لطفاً چند لحظه صبر کنید.";

                _lesson = await _dataService.GetLessonByIdAsync(_lessonId);

                if (_lesson == null)
                {
                    MessageBox.Show(
                        this,
                        "درس مورد نظر پیدا نشد یا غیرفعال است.",
                        "درس",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Close();
                    return;
                }

                Title = "درس: " + SafeText(_lesson.Title);

                LessonTitleText.Text = SafeText(_lesson.Title);

                LessonContentText.Text = string.IsNullOrWhiteSpace(_lesson.Content)
                    ? "برای این درس محتوایی ثبت نشده است."
                    : _lesson.Content.Trim();

                var estimatedMinutes = _lesson.EstimatedMinutes > 0
                    ? _lesson.EstimatedMinutes
                    : 90;

                var passingScore = _lesson.PassingScore > 0
                    ? _lesson.PassingScore
                    : 60;

                LessonStatusText.Text =
                    $"نوع درس: {SafeText(_lesson.LessonType)} | " +
                    $"سطح: {_lesson.Level} | " +
                    $"زمان تقریبی: {estimatedMinutes} دقیقه | " +
                    $"حد نصاب آزمون: {passingScore} از ۱۰۰";

                var hasVideo = MediaPathService.Exists(_lesson.VideoPath);
                var hasAudio = MediaPathService.Exists(_lesson.AudioPath);
                var hasPdf = MediaPathService.Exists(_lesson.PdfPath);

                VideoStatusText.Text = hasVideo
                    ? "🎬 ویدئو: موجود"
                    : "🎬 ویدئو: ندارد";

                AudioStatusText.Text = hasAudio
                    ? "🎧 صوت: موجود"
                    : "🎧 صوت: ندارد";

                PdfStatusText.Text = hasPdf
                    ? "📄 PDF: موجود"
                    : "📄 PDF: ندارد";

                SetButtonsEnabled(true);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "هنگام بارگذاری درس خطایی رخ داد:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "خطای درس",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }
        }

        private async void ExerciseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_lesson == null)
            {
                return;
            }

            try
            {
                SetButtonsEnabled(false);

                var questions =
                    await _dataService.GetQuestionsAsync(_lesson.Id);

                if (questions.Count == 0)
                {
                    MessageBox.Show(
                        this,
                        "برای این درس سؤالی ثبت نشده است." +
                        Environment.NewLine +
                        "در صورت مطالعه کامل، می‌توانید از گزینه «ثبت مطالعه بدون آزمون» استفاده کنید.",
                        "تمرین",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var quizWindow = new QuizWindow(
                    questions,
                    _lesson.Title)
                {
                    Owner = this
                };

                var result = quizWindow.ShowDialog();

                if (result == true && quizWindow.IsCompleted)
                {
                    var finalScore = quizWindow.FinalScore;

                    var passingScore = _lesson.PassingScore > 0
                        ? _lesson.PassingScore
                        : 60;

                    var passed = finalScore >= passingScore;

                    await _dataService.SaveLessonProgressAsync(
                        _lesson.Id,
                        passed,
                        finalScore);

                    if (passed)
                    {
                        MessageBox.Show(
                            this,
                            $"آفرین! نمره شما {finalScore} از ۱۰۰ است و این درس کامل شد. 🎉",
                            "نتیجه تمرین",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(
                            this,
                            $"نمره شما {finalScore} از ۱۰۰ است." +
                            Environment.NewLine +
                            $"برای تکمیل درس باید حداقل {passingScore} بگیرید.",
                            "نتیجه تمرین",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "هنگام اجرای تمرین خطایی رخ داد:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "خطای تمرین",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async void MarkCompletedButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_lesson == null)
            {
                return;
            }

            var confirm = MessageBox.Show(
                this,
                "آیا مطمئن هستید که این درس را مطالعه کرده‌اید؟" +
                Environment.NewLine +
                "با تأیید، این درس بدون آزمون به‌عنوان کامل‌شده ثبت می‌شود.",
                "ثبت مطالعه درس",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                SetButtonsEnabled(false);

                await _dataService.SaveLessonProgressAsync(
                    _lesson.Id,
                    true,
                    100);

                MessageBox.Show(
                    this,
                    "مطالعه درس با موفقیت ثبت شد. ✅",
                    "ثبت شد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "هنگام ثبت پیشرفت درس خطایی رخ داد:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "خطای ثبت پیشرفت",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void LessonReaderWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _dbContext.Dispose();
        }

        private void SetButtonsEnabled(bool isEnabled)
        {
            ExerciseButton.IsEnabled = isEnabled;
            MarkCompletedButton.IsEnabled = isEnabled;
        }

        private static string SafeText(string? text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? "ثبت نشده"
                : text.Trim();
        }
    }
}

