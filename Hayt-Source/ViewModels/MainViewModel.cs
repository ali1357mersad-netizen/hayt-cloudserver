using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Hayt.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly ICurrentUserService _currentUserService;

        private Category? _selectedCategory;
        private Book? _selectedBook;
        private Section? _selectedSection;
        private Chapter? _selectedChapter;
        private Lesson? _selectedLesson;
        private Question? _selectedQuestion;

        private string _stepInfo = "مرحله ۰ از ۰";
        private string _lessonTitle = "اندیشکده حیات";
        private string _lessonContent = "در حال آماده‌سازی اطلاعات...";
        private string _lessonStatus = "لطفاً یک کتاب و درس را انتخاب کنید.";
        private string _mediaStatus = "برای این درس رسانه‌ای ثبت نشده است.";
        private string _offlineStatus = "نسخه آفلاین";
        private string _selectedCategoryTitle = "همه کتاب‌ها";

        private bool _isBusy;
        private bool _categoryPanelOpen;

        public MainViewModel(IDataService dataService, ICurrentUserService currentUserService)
        {
            _dataService = dataService;
            _currentUserService = currentUserService;

            PreviousLessonCommand = new RelayCommand(PreviousLesson, CanGoPrevious);
            NextLessonCommand = new RelayCommand(NextLesson, CanGoNext);
            RestartCommand = new RelayCommand(Restart);
            ExerciseCommand = new RelayCommand(async () => await ExerciseAsync());
            MediaCommand = new RelayCommand(async () => await OpenMediaAsync());
            StatsCommand = new RelayCommand(async () => await ShowStatsAsync());
            ManageCommand = new RelayCommand(async () => await ManageAsync());
            ProgressReportCommand = new RelayCommand(OpenProgressReport);
            ProfileCommand = new RelayCommand(OpenUserProfile);
            ChangeUserCommand = new RelayCommand(async () => await ChangeUserAsync());
            RefreshCommand = new RelayCommand(async () => await InitializeAsync());
            SelectCategoryCommand = new RelayCommand(async (param) => await SelectCategoryAsync(param as string), null);
            ToggleCategoryPanelCommand = new RelayCommand(ToggleCategoryPanel);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
        }

        public ObservableCollection<Category> Categories { get; } = new();

        public ObservableCollection<Book> Books { get; } = new();

        public ObservableCollection<Section> Sections { get; } = new();

        public ObservableCollection<Chapter> Chapters { get; } = new();

        public ObservableCollection<Lesson> Lessons { get; } = new();

        public ObservableCollection<Question> Questions { get; } = new();

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    SelectedCategoryTitle = value?.Title ?? "همه کتاب‌ها";
                    _ = LoadBooksByCategoryAsync(value?.Id);
                }
            }
        }

        public string SelectedCategoryTitle
        {
            get => _selectedCategoryTitle;
            set => SetProperty(ref _selectedCategoryTitle, value);
        }

        public bool CategoryPanelOpen
        {
            get => _categoryPanelOpen;
            set => SetProperty(ref _categoryPanelOpen, value);
        }

        public Book? SelectedBook
        {
            get => _selectedBook;
            set
            {
                if (SetProperty(ref _selectedBook, value))
                {
                    _ = LoadSectionsAsync(value);
                }
            }
        }

        public Section? SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (SetProperty(ref _selectedSection, value))
                {
                    _ = LoadChaptersAsync(value);
                }
            }
        }

        public Chapter? SelectedChapter
        {
            get => _selectedChapter;
            set
            {
                if (SetProperty(ref _selectedChapter, value))
                {
                    _ = LoadLessonsAsync(value);
                }
            }
        }

        public Lesson? SelectedLesson
        {
            get => _selectedLesson;
            set
            {
                if (SetProperty(ref _selectedLesson, value))
                {
                    _ = LoadQuestionsAndShowLessonAsync(value);
                    RaiseNavigationCommands();
                }
            }
        }

        public Question? SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                if (SetProperty(ref _selectedQuestion, value))
                {
                    if (value != null)
                    {
                        LessonStatus = $"سؤال انتخاب‌شده: {value.QuestionText}";
                    }
                }
            }
        }

        public string StepInfo
        {
            get => _stepInfo;
            set => SetProperty(ref _stepInfo, value);
        }

        public string LessonTitle
        {
            get => _lessonTitle;
            set => SetProperty(ref _lessonTitle, value);
        }

        public string LessonContent
        {
            get => _lessonContent;
            set => SetProperty(ref _lessonContent, value);
        }

        public string LessonStatus
        {
            get => _lessonStatus;
            set => SetProperty(ref _lessonStatus, value);
        }

        public string MediaStatus
        {
            get => _mediaStatus;
            set => SetProperty(ref _mediaStatus, value);
        }

        public string OfflineStatus
        {
            get => _offlineStatus;
            set => SetProperty(ref _offlineStatus, value);
        }
        public string CurrentUserDisplayName
        {
            get
            {
                var name = _currentUserService.CurrentUser?.DisplayName;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "کاربر اصلی";
                }

                return "👤 " + name;
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand PreviousLessonCommand { get; }

        public ICommand NextLessonCommand { get; }

        public ICommand RestartCommand { get; }

        public ICommand ExerciseCommand { get; }

        public ICommand MediaCommand { get; }

        public ICommand StatsCommand { get; }

        public ICommand ManageCommand { get; }

        public ICommand ProgressReportCommand { get; }

        public ICommand ProfileCommand { get; }

        public ICommand ChangeUserCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand SelectCategoryCommand { get; }
        public ICommand ToggleCategoryPanelCommand { get; }
        public ICommand ToggleThemeCommand { get; }


        private void ToggleTheme()
        {
            ThemeService.Instance.ToggleTheme();
        }
    }
}


