using Hayt.Models;
using Hayt.Licensing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hayt.Views
{
    public partial class QuizWindow : Window
    {
        private readonly List<Question> _questions;
        private readonly string _lessonTitle;

        private readonly List<RadioButton> _radioButtons;
        private readonly List<Border> _optionBorders;

        /*
         * مقدار -1 یعنی این سؤال هنوز پاسخ داده نشده است.
         * مقدار 0 تا 3 یعنی گزینه انتخاب‌شده کاربر.
         */
        private readonly int[] _selectedAnswers;

        /*
         * برای جلوگیری از افزایش دوباره نمره در رفت‌وبرگشت بین سؤال‌ها.
         */
        private readonly bool[] _isCorrectAnswer;

        /*
         * برای تشخیص اینکه سؤال پاسخ داده شده یا نه.
         */
        private readonly bool[] _isAnswered;

        private int _currentIndex;
        private int _correctCount;

        private static readonly Brush NormalBackground =
            new SolidColorBrush(Color.FromRgb(249, 250, 251));

        private static readonly Brush NormalBorderBrush =
            new SolidColorBrush(Color.FromRgb(229, 231, 235));

        private static readonly Brush CorrectBackground =
            new SolidColorBrush(Color.FromRgb(220, 252, 231));

        private static readonly Brush CorrectBorderBrush =
            new SolidColorBrush(Color.FromRgb(34, 197, 94));

        private static readonly Brush WrongBackground =
            new SolidColorBrush(Color.FromRgb(254, 226, 226));

        private static readonly Brush WrongBorderBrush =
            new SolidColorBrush(Color.FromRgb(239, 68, 68));

        private static readonly Brush SelectedBackground =
            new SolidColorBrush(Color.FromRgb(219, 234, 254));

        private static readonly Brush SelectedBorderBrush =
            new SolidColorBrush(Color.FromRgb(37, 99, 235));

        public int FinalScore { get; private set; }

        public bool IsCompleted { get; private set; }

        public QuizWindow(IEnumerable<Question> questions, string lessonTitle)
        {
            InitializeComponent();

            _questions = questions?
                .OrderBy(q => q.OrderNumber)
                .ThenBy(q => q.Id)
                .ToList()
                ?? new List<Question>();

            _lessonTitle = string.IsNullOrWhiteSpace(lessonTitle)
                ? "درس"
                : lessonTitle.Trim();

            _selectedAnswers = Enumerable.Repeat(-1, _questions.Count).ToArray();
            _isCorrectAnswer = new bool[_questions.Count];
            _isAnswered = new bool[_questions.Count];

            _radioButtons = new List<RadioButton>
            {
                OptionRadio0,
                OptionRadio1,
                OptionRadio2,
                OptionRadio3
            };

            _optionBorders = new List<Border>
            {
                OptionBorder0,
                OptionBorder1,
                OptionBorder2,
                OptionBorder3
            };

            TitleTextBlock.Text = $"آزمون: {_lessonTitle}";
            Title = $"آزمون: {_lessonTitle}";

            if (_questions.Count == 0)
            {
                MessageBox.Show(
                    "برای این درس سؤالی ثبت نشده است.",
                    "آزمون",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = false;
                Close();
                return;
            }

            LoadQuestion();
        }

        private void LoadQuestion()
        {
            if (_questions.Count == 0)
            {
                return;
            }

            var question = _questions[_currentIndex];

            QuestionTextBlock.Text = string.IsNullOrWhiteSpace(question.QuestionText)
                ? "متن سؤال ثبت نشده است."
                : question.QuestionText;

            _radioButtons[0].Content = "۱) " + SafeText(question.OptionA);
            _radioButtons[1].Content = "۲) " + SafeText(question.OptionB);
            _radioButtons[2].Content = "۳) " + SafeText(question.OptionC);
            _radioButtons[3].Content = "۴) " + SafeText(question.OptionD);

            ResetOptionVisuals();

            var selectedIndex = _selectedAnswers[_currentIndex];

            foreach (var radio in _radioButtons)
            {
                radio.IsChecked = false;
                radio.IsEnabled = !_isAnswered[_currentIndex] && !IsCompleted;
            }

            if (selectedIndex >= 0 && selectedIndex <= 3)
            {
                _radioButtons[selectedIndex].IsChecked = true;
            }

            if (_isAnswered[_currentIndex])
            {
                ShowAnsweredState();
                SubmitButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                ResultTextBlock.Visibility = Visibility.Collapsed;
                ResultTextBlock.Text = string.Empty;
                SubmitButton.Visibility = IsCompleted ? Visibility.Collapsed : Visibility.Visible;
            }

            UpdateNavigationButtons();
            UpdateProgressText();
            UpdateScoreText();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsCompleted)
            {
                return;
            }

            if (_isAnswered[_currentIndex])
            {
                return;
            }

            var selectedIndex = GetSelectedOptionIndex();

            if (selectedIndex < 0)
            {
                MessageBox.Show(
                    "لطفاً یک گزینه را انتخاب کنید.",
                    "ثبت پاسخ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var question = _questions[_currentIndex];
            var correctIndex = NormalizeCorrectIndex(question.CorrectOptionIndex);

            _selectedAnswers[_currentIndex] = selectedIndex;
            _isAnswered[_currentIndex] = true;

            var isCorrect = selectedIndex == correctIndex;
            _isCorrectAnswer[_currentIndex] = isCorrect;

            if (isCorrect)
            {
                _correctCount++;
            }

            foreach (var radio in _radioButtons)
            {
                radio.IsEnabled = false;
            }

            ShowAnsweredState();

            SubmitButton.Visibility = Visibility.Collapsed;

            if (AreAllQuestionsAnswered())
            {
                FinishQuiz(showMessage: false);
            }

            UpdateNavigationButtons();
            UpdateProgressText();
            UpdateScoreText();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex <= 0)
            {
                return;
            }

            _currentIndex--;
            LoadQuestion();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex >= _questions.Count - 1)
            {
                return;
            }

            _currentIndex++;
            LoadQuestion();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsCompleted)
            {
                var unansweredCount = _isAnswered.Count(x => !x);

                if (unansweredCount > 0)
                {
                    var confirm = MessageBox.Show(
                        $"هنوز به {ToPersianNumber(unansweredCount)} سؤال پاسخ نداده‌اید." +
                        Environment.NewLine +
                        "آیا می‌خواهید آزمون را با همین وضعیت پایان دهید؟",
                        "پایان آزمون",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                FinishQuiz(showMessage: true);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsCompleted && _isAnswered.Any(x => x))
            {
                var confirm = MessageBox.Show(
                    "آزمون هنوز کامل نشده است." +
                    Environment.NewLine +
                    "اگر ببندید، نتیجه آزمون ذخیره نمی‌شود." +
                    Environment.NewLine +
                    "آیا مطمئن هستید؟",
                    "بستن آزمون",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            DialogResult = IsCompleted;
            Close();
        }

        private void FinishQuiz(bool showMessage)
        {
            IsCompleted = true;

            FinalScore = _questions.Count == 0
                ? 0
                : (int)Math.Round(_correctCount * 100.0 / _questions.Count);

            foreach (var radio in _radioButtons)
            {
                radio.IsEnabled = false;
            }

            SubmitButton.Visibility = Visibility.Collapsed;
            FinishButton.Visibility = Visibility.Visible;
            FinishButton.Content = "ثبت نتیجه و خروج";

            ShowAnsweredState();
            UpdateNavigationButtons();
            UpdateProgressText();
            UpdateScoreText();

            if (showMessage)
            {
                MessageBox.Show(
                    "آزمون پایان یافت." +
                    Environment.NewLine +
                    "نمره شما: " +
                    ToPersianNumber(FinalScore) +
                    " از ۱۰۰",
                    "نتیجه آزمون",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ShowAnsweredState()
        {
            ResetOptionVisuals();

            var question = _questions[_currentIndex];
            var correctIndex = NormalizeCorrectIndex(question.CorrectOptionIndex);
            var selectedIndex = _selectedAnswers[_currentIndex];

            foreach (var radio in _radioButtons)
            {
                radio.IsEnabled = false;
            }

            if (correctIndex >= 0 && correctIndex <= 3)
            {
                _optionBorders[correctIndex].Background = CorrectBackground;
                _optionBorders[correctIndex].BorderBrush = CorrectBorderBrush;
            }

            if (selectedIndex >= 0 && selectedIndex <= 3)
            {
                _radioButtons[selectedIndex].IsChecked = true;

                if (selectedIndex != correctIndex)
                {
                    _optionBorders[selectedIndex].Background = WrongBackground;
                    _optionBorders[selectedIndex].BorderBrush = WrongBorderBrush;
                }
            }

            var explanation = string.IsNullOrWhiteSpace(question.Explanation)
                ? "توضیحی برای این سؤال ثبت نشده است."
                : question.Explanation.Trim();

            if (selectedIndex == correctIndex)
            {
                ResultTextBlock.Foreground =
                    new SolidColorBrush(Color.FromRgb(22, 163, 74));

                ResultTextBlock.Text =
                    "✅ پاسخ درست است." +
                    Environment.NewLine +
                    explanation;
            }
            else
            {
                ResultTextBlock.Foreground =
                    new SolidColorBrush(Color.FromRgb(220, 38, 38));

                ResultTextBlock.Text =
                    "❌ پاسخ نادرست است." +
                    Environment.NewLine +
                    "پاسخ صحیح: گزینه " +
                    ToPersianNumber(correctIndex + 1) +
                    Environment.NewLine +
                    explanation;
            }

            if (IsCompleted && _currentIndex == _questions.Count - 1)
            {
                ResultTextBlock.Text +=
                    Environment.NewLine +
                    Environment.NewLine +
                    "آزمون تمام شد." +
                    Environment.NewLine +
                    "نمره شما: " +
                    ToPersianNumber(FinalScore) +
                    " از ۱۰۰";
            }

            ResultTextBlock.Visibility = Visibility.Visible;
        }

        private void ResetOptionVisuals()
        {
            for (var i = 0; i < _optionBorders.Count; i++)
            {
                _optionBorders[i].Background = NormalBackground;
                _optionBorders[i].BorderBrush = NormalBorderBrush;
            }
        }

        private void UpdateNavigationButtons()
        {
            PreviousButton.IsEnabled = _currentIndex > 0;
            NextButton.IsEnabled = _currentIndex < _questions.Count - 1;

            if (IsCompleted)
            {
                SubmitButton.Visibility = Visibility.Collapsed;
                FinishButton.Visibility = Visibility.Visible;
                FinishButton.Content = "ثبت نتیجه و خروج";
                return;
            }

            FinishButton.Visibility = Visibility.Visible;

            if (AreAllQuestionsAnswered())
            {
                FinishButton.Content = "پایان آزمون";
                FinishButton.Background =
                    new SolidColorBrush(Color.FromRgb(22, 163, 74));
            }
            else
            {
                FinishButton.Content = "پایان آزمون";
                FinishButton.Background =
                    new SolidColorBrush(Color.FromRgb(22, 163, 74));
            }

            if (_isAnswered[_currentIndex])
            {
                SubmitButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                SubmitButton.Visibility = Visibility.Visible;
            }
        }

        private void UpdateProgressText()
        {
            var answeredCount = _isAnswered.Count(x => x);

            ProgressTextBlock.Text =
                $"سؤال {ToPersianNumber(_currentIndex + 1)} از {ToPersianNumber(_questions.Count)}";

            AnsweredCountTextBlock.Text =
                $"پاسخ‌داده‌شده: {ToPersianNumber(answeredCount)} از {ToPersianNumber(_questions.Count)}";

            QuizProgressBar.Value = _questions.Count == 0
                ? 0
                : answeredCount * 100.0 / _questions.Count;
        }

        private void UpdateScoreText()
        {
            var answeredCount = _isAnswered.Count(x => x);

            var currentScore = _questions.Count == 0
                ? 0
                : (int)Math.Round(_correctCount * 100.0 / _questions.Count);

            ScoreTextBlock.Text =
                $"درست: {ToPersianNumber(_correctCount)} | " +
                $"پاسخ‌داده‌شده: {ToPersianNumber(answeredCount)} | " +
                $"نمره فعلی: {ToPersianNumber(currentScore)}";
        }

        private int GetSelectedOptionIndex()
        {
            for (var i = 0; i < _radioButtons.Count; i++)
            {
                if (_radioButtons[i].IsChecked == true)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool AreAllQuestionsAnswered()
        {
            return _questions.Count > 0 && _isAnswered.All(x => x);
        }

        private static int NormalizeCorrectIndex(int index)
        {
            return index < 0 || index > 3
                ? 0
                : index;
        }

        private static string SafeText(string? text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? "گزینه ثبت نشده"
                : text.Trim();
        }

        private void SelectOption(int index)
        {
            if (IsCompleted)
            {
                return;
            }

            if (_isAnswered[_currentIndex])
            {
                return;
            }

            if (index < 0 || index > 3)
            {
                return;
            }

            for (var i = 0; i < _radioButtons.Count; i++)
            {
                _radioButtons[i].IsChecked = i == index;
            }

            ResetOptionVisuals();

            _optionBorders[index].Background = SelectedBackground;
            _optionBorders[index].BorderBrush = SelectedBorderBrush;
        }

        private void OptionBorder0_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectOption(0);
        }

        private void OptionBorder1_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectOption(1);
        }

        private void OptionBorder2_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectOption(2);
        }

        private void OptionBorder3_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectOption(3);
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
    }
}

