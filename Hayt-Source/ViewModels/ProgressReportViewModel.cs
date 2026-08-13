using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hayt.ViewModels
{
    public class ProgressReportViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        private bool _isLoading;
        private string _statusMessage = string.Empty;

        private int _totalBooks;
        private int _totalLessons;
        private int _completedLessons;
        private int _completedBooks;
        private double _overallCompletionPercent;
        private double _totalCompletedHours;
        private int _totalEarnedXp;
        private string _overallLevelTitle = "نوآموز";

        public ProgressReportViewModel(IDataService dataService)
        {
            _dataService = dataService ??
                throw new ArgumentNullException(nameof(dataService));

            RefreshCommand = new RelayCommand(
                async () => await LoadAsync(),
                () => !IsLoading);

            StatusMessage = "در حال آماده‌سازی گزارش پیشرفت...";
        }

        public ObservableCollection<BookProgressReport> Reports { get; } = new();

        public ICommand RefreshCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;
                OnPropertyChanged();

                if (RefreshCommand is RelayCommand command)
                {
                    command.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value)
                    return;

                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int TotalBooks
        {
            get => _totalBooks;
            private set
            {
                if (_totalBooks == value)
                    return;

                _totalBooks = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalBooksText));
            }
        }

        public int TotalLessons
        {
            get => _totalLessons;
            private set
            {
                if (_totalLessons == value)
                    return;

                _totalLessons = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LessonsSummaryText));
            }
        }

        public int CompletedLessons
        {
            get => _completedLessons;
            private set
            {
                if (_completedLessons == value)
                    return;

                _completedLessons = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LessonsSummaryText));
            }
        }

        public int CompletedBooks
        {
            get => _completedBooks;
            private set
            {
                if (_completedBooks == value)
                    return;

                _completedBooks = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CertificatesText));
            }
        }

        public double OverallCompletionPercent
        {
            get => _overallCompletionPercent;
            private set
            {
                if (Math.Abs(_overallCompletionPercent - value) < 0.001)
                    return;

                _overallCompletionPercent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OverallPercentText));
            }
        }

        public double TotalCompletedHours
        {
            get => _totalCompletedHours;
            private set
            {
                if (Math.Abs(_totalCompletedHours - value) < 0.001)
                    return;

                _totalCompletedHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalHoursText));
            }
        }

        public int TotalEarnedXp
        {
            get => _totalEarnedXp;
            private set
            {
                if (_totalEarnedXp == value)
                    return;

                _totalEarnedXp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalXpText));
            }
        }

        public string OverallLevelTitle
        {
            get => _overallLevelTitle;
            private set
            {
                if (_overallLevelTitle == value)
                    return;

                _overallLevelTitle = value;
                OnPropertyChanged();
            }
        }

        public string TotalBooksText =>
            $"{TotalBooks} کتاب";

        public string LessonsSummaryText =>
            $"{CompletedLessons} از {TotalLessons} درس";

        public string OverallPercentText =>
            $"{OverallCompletionPercent:0.#}٪";

        public string TotalHoursText =>
            $"{TotalCompletedHours:0.#} ساعت";

        public string TotalXpText =>
            $"{TotalEarnedXp:N0} امتیاز";

        public string CertificatesText =>
            $"{CompletedBooks} گواهی آماده";

        public async Task LoadAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "در حال بارگذاری گزارش پیشرفت...";

                var reports = await _dataService.GetBookProgressReportsAsync();

                Reports.Clear();

                foreach (var report in reports)
                {
                    Reports.Add(report);
                }

                CalculateSummary();

                StatusMessage = reports.Count == 0
                    ? "هنوز کتابی برای نمایش گزارش وجود ندارد."
                    : $"گزارش پیشرفت {reports.Count} کتاب با موفقیت بارگذاری شد.";
            }
            catch (Exception ex)
            {
                Reports.Clear();
                ResetSummary();

                StatusMessage =
                    "خطا در بارگذاری گزارش پیشرفت: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateSummary()
        {
            TotalBooks = Reports.Count;
            TotalLessons = Reports.Sum(report => report.TotalLessons);
            CompletedLessons = Reports.Sum(report => report.CompletedLessons);
            CompletedBooks = Reports.Count(report => report.IsCompleted);
            TotalCompletedHours = Reports.Sum(report => report.CompletedHours);
            TotalEarnedXp = Reports.Sum(report => report.EarnedXp);

            OverallCompletionPercent = TotalLessons == 0
                ? 0
                : CompletedLessons * 100.0 / TotalLessons;

            OverallLevelTitle =
                BookProgressReport.GetLevelTitle(TotalEarnedXp);
        }

        private void ResetSummary()
        {
            TotalBooks = 0;
            TotalLessons = 0;
            CompletedLessons = 0;
            CompletedBooks = 0;
            OverallCompletionPercent = 0;
            TotalCompletedHours = 0;
            TotalEarnedXp = 0;
            OverallLevelTitle = BookProgressReport.GetLevelTitle(0);
        }
    }
}


