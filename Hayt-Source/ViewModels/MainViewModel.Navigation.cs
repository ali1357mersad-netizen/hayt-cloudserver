using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Hayt.ViewModels
{
    public partial class MainViewModel
    {
        private bool CanGoPrevious()
        {
            if (SelectedLesson == null)
            {
                return false;
            }

            var index = Lessons.IndexOf(SelectedLesson);

            return index > 0;
        }

        private bool CanGoNext()
        {
            if (SelectedLesson == null)
            {
                return false;
            }

            var index = Lessons.IndexOf(SelectedLesson);

            return index >= 0 && index < Lessons.Count - 1;
        }

        private void PreviousLesson()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            var index = Lessons.IndexOf(SelectedLesson);

            if (index > 0)
            {
                SelectedLesson = Lessons[index - 1];
            }
        }

        private void NextLesson()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            var index = Lessons.IndexOf(SelectedLesson);

            if (index >= 0 && index < Lessons.Count - 1)
            {
                SelectedLesson = Lessons[index + 1];
            }
        }

        private void Restart()
        {
            SelectedCategory = null;

            if (Books.Count > 0)
            {
                SelectedBook = Books.FirstOrDefault();
            }

            if (Sections.Count > 0)
            {
                SelectedSection = Sections.FirstOrDefault();
            }

            if (Chapters.Count > 0)
            {
                SelectedChapter = Chapters.FirstOrDefault();
            }

            if (Lessons.Count > 0)
            {
                SelectedLesson = Lessons.FirstOrDefault();
            }
        }

        private Task OpenMediaAsync()
        {
            if (SelectedLesson == null)
            {
                MessageBox.Show(
                    "ابتدا یک درس انتخاب کنید.",
                    "رسانه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return Task.CompletedTask;
            }

            var hasVideo = MediaPathService.Exists(SelectedLesson.VideoPath);
            var hasAudio = MediaPathService.Exists(SelectedLesson.AudioPath);
            var hasPdf = MediaPathService.Exists(SelectedLesson.PdfPath);

            if (!hasVideo && !hasAudio && !hasPdf)
            {
                MessageBox.Show(
                    "برای این درس فایل صوت، ویدئو یا PDF پیدا نشد.",
                    "رسانه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return Task.CompletedTask;
            }

            var mediaWindow = new MediaWindow(SelectedLesson)
            {
                Owner = Application.Current.MainWindow
            };

            mediaWindow.ShowDialog();

            return Task.CompletedTask;
        }

        private async Task ExerciseAsync()
        {
            if (SelectedLesson == null)
            {
                MessageBox.Show(
                    "ابتدا یک درس انتخاب کنید.",
                    "تمرین",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (Questions.Count == 0)
            {
                MessageBox.Show(
                    "برای این درس سؤالی ثبت نشده است.",
                    "تمرین",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var quizWindow = new QuizWindow(
                Questions.ToList(),
                SelectedLesson.Title)
            {
                Owner = Application.Current.MainWindow
            };

            var result = quizWindow.ShowDialog();

            if (result == true && quizWindow.IsCompleted)
            {
                var passed = quizWindow.FinalScore >= SelectedLesson.PassingScore;

                await _dataService.SaveLessonProgressAsync(
                    SelectedLesson.Id,
                    passed,
                    quizWindow.FinalScore);

                LessonStatus =
                    $"آزمون انجام شد | نمره: {ToPersianNumber(quizWindow.FinalScore)} | " +
                    $"وضعیت: {(passed ? "قبول" : "نیاز به تلاش دوباره")}";
            }
        }

        private async Task ShowStatsAsync()
        {
            var total = await _dataService.GetTotalLessonCountAsync();
            var completed = await _dataService.GetCompletedLessonCountAsync();

            var percent = total == 0 ? 0 : completed * 100.0 / total;

            var message =
                "آمار پیشرفت شما" + Environment.NewLine + Environment.NewLine +
                "کل درس‌ها: " + ToPersianNumber(total) + Environment.NewLine +
                "درس‌های تکمیل‌شده: " + ToPersianNumber(completed) + Environment.NewLine +
                "درصد پیشرفت: " + ToPersianNumber(percent.ToString("0.0")) + "٪";

            MessageBox.Show(message, "آمار", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenProgressReport()
        {
            var window = new ProgressReportWindow(_currentUserService)
            {
                Owner = Application.Current.MainWindow
            };

            window.ShowDialog();
        }

        private void OpenUserProfile()
        {
            var window = new UserProfileWindow(_currentUserService)
            {
                Owner = Application.Current.MainWindow
            };

            window.ShowDialog();
        }

        private async Task ChangeUserAsync()
        {
            var window = new UserSelectionWindow(_currentUserService)
            {
                Owner = Application.Current.MainWindow
            };

            var result = window.ShowDialog();

            if (result == true && window.UserChanged)
            {
                await InitializeAsync();
                OnPropertyChanged(nameof(CurrentUserDisplayName));

                MessageBox.Show(
                    "کاربر فعال تغییر کرد. اطلاعات برنامه برای کاربر جدید به‌روزرسانی شد.",
                    "تغییر کاربر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async Task ManageAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "انتخاب فایل JSON کتاب",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                await _dataService.ImportBookFromJsonAsync(dialog.FileName);
                await LoadBooksAsync();

                MessageBox.Show("کتاب با موفقیت وارد شد.", "مدیریت کتاب‌ها", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ToggleCategoryPanel()
        {
            CategoryPanelOpen = !CategoryPanelOpen;
        }

        private void RaiseNavigationCommands()
        {
            if (PreviousLessonCommand is RelayCommand previous)
            {
                previous.RaiseCanExecuteChanged();
            }

            if (NextLessonCommand is RelayCommand next)
            {
                next.RaiseCanExecuteChanged();
            }

            if (SelectCategoryCommand is RelayCommand catCmd)
            {
                catCmd.RaiseCanExecuteChanged();
            }
        }

        private static string ToPersianNumber(object? input)
        {
            var text = input?.ToString() ?? string.Empty;

            return text
                .Replace("0", "۰")
                .Replace("1", "۱")
                .Replace("2", "۲")
                .Replace("3", "۳")
                .Replace("4", "۴")
                .Replace("5", "۵")
                .Replace("6", "۶")
                .Replace("7", "۷")
                .Replace("8", "۸")
                .Replace("9", "۹");
        }
    }}


