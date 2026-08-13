using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Hayt.ViewModels
{
    public partial class MainViewModel
    {
        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                LessonTitle = "در حال بارگذاری کتابخانه...";
                LessonContent = "لطفاً چند لحظه صبر کنید.";
                LessonStatus = "آماده‌سازی پایگاه داده و کتاب‌ها";
                StepInfo = "مرحله ۰ از ۰";

                await _dataService.ImportSeedBooksAsync();

                await LoadCategoriesAsync();
                await LoadBooksAsync();
            }
            catch (Exception ex)
            {
                LessonTitle = "خطا در بارگذاری";
                LessonContent = ex.Message;
                LessonStatus = "خطا رخ داد.";
                MessageBox.Show(ex.Message, "خطا در Initialize", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            Categories.Clear();

            var categories = await _dataService.GetCategoriesAsync();

            foreach (var cat in categories)
            {
                Categories.Add(cat);
            }
        }

        private async Task LoadBooksAsync()
        {
            Books.Clear();
            Sections.Clear();
            Chapters.Clear();
            Lessons.Clear();
            Questions.Clear();

            SelectedBook = null;
            SelectedSection = null;
            SelectedChapter = null;
            SelectedLesson = null;
            SelectedQuestion = null;

            var books = await _dataService.GetBooksAsync();

            foreach (var book in books)
            {
                Books.Add(book);
            }

            if (Books.Count == 0)
            {
                LessonTitle = "کتابی یافت نشد";
                LessonContent = "هیچ کتابی در پایگاه داده پیدا نشد. لطفاً از بخش مدیریت، فایل JSON کتاب را وارد کنید.";
                LessonStatus = "آماده دریافت کتاب";
                StepInfo = "مرحله ۰ از ۰";
                MediaStatus = "رسانه‌ای وجود ندارد.";
                return;
            }

            SelectedBook = Books.FirstOrDefault();
        }

        private async Task LoadBooksByCategoryAsync(string? categoryId)
        {
            Books.Clear();
            Sections.Clear();
            Chapters.Clear();
            Lessons.Clear();
            Questions.Clear();

            SelectedBook = null;
            SelectedSection = null;
            SelectedChapter = null;
            SelectedLesson = null;
            SelectedQuestion = null;

            List<Book> books;

            if (string.IsNullOrWhiteSpace(categoryId))
            {
                books = await _dataService.GetBooksAsync();
            }
            else
            {
                books = await _dataService.GetBooksByCategoryAsync(categoryId);
            }

            foreach (var book in books)
            {
                Books.Add(book);
            }

            if (Books.Count == 0)
            {
                LessonTitle = "کتابی در این دسته یافت نشد";
                LessonContent = $"در دسته «{SelectedCategoryTitle}» کتابی وجود ندارد.";
                LessonStatus = "دسته خالی است";
                StepInfo = "مرحله ۰ از ۰";
                MediaStatus = "رسانه‌ای وجود ندارد.";
                return;
            }

            SelectedBook = Books.FirstOrDefault();
        }

        private Task SelectCategoryAsync(string? categoryId)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                SelectedCategory = null;
                return Task.CompletedTask;
            }

            var category = Categories.FirstOrDefault(c => c.Id == categoryId);
            SelectedCategory = category;
            return Task.CompletedTask;
        }

        private async Task LoadSectionsAsync(Book? book)
        {
            Sections.Clear();
            Chapters.Clear();
            Lessons.Clear();
            Questions.Clear();

            SelectedSection = null;
            SelectedChapter = null;
            SelectedLesson = null;
            SelectedQuestion = null;

            if (book == null)
            {
                return;
            }

            var sections = await _dataService.GetSectionsAsync(book.Id);

            foreach (var section in sections)
            {
                Sections.Add(section);
            }

            SelectedSection = Sections.FirstOrDefault();
        }

        private async Task LoadChaptersAsync(Section? section)
        {
            Chapters.Clear();
            Lessons.Clear();
            Questions.Clear();

            SelectedChapter = null;
            SelectedLesson = null;
            SelectedQuestion = null;

            if (section == null)
            {
                return;
            }

            var chapters = await _dataService.GetChaptersAsync(section.Id);

            foreach (var chapter in chapters)
            {
                Chapters.Add(chapter);
            }

            SelectedChapter = Chapters.FirstOrDefault();
        }

        private async Task LoadLessonsAsync(Chapter? chapter)
        {
            Lessons.Clear();
            Questions.Clear();

            SelectedLesson = null;
            SelectedQuestion = null;

            if (chapter == null)
            {
                return;
            }

            var lessons = await _dataService.GetLessonsAsync(chapter.Id);

            foreach (var lesson in lessons)
            {
                Lessons.Add(lesson);
            }

            SelectedLesson = Lessons.FirstOrDefault();
        }

        private async Task LoadQuestionsAndShowLessonAsync(Lesson? lesson)
        {
            Questions.Clear();
            SelectedQuestion = null;

            if (lesson == null)
            {
                LessonTitle = "درسی انتخاب نشده است";
                LessonContent = "";
                LessonStatus = "لطفاً یک درس را انتخاب کنید.";
                StepInfo = "مرحله ۰ از ۰";
                MediaStatus = "برای این درس رسانه‌ای ثبت نشده است.";
                return;
            }

            var questions = await _dataService.GetQuestionsAsync(lesson.Id);

            foreach (var question in questions)
            {
                Questions.Add(question);
            }

            SelectedQuestion = Questions.FirstOrDefault();

            LessonTitle = lesson.Title;
            LessonContent = lesson.Content;

            var index = Lessons.IndexOf(lesson) + 1;
            var total = Lessons.Count;

            StepInfo = $"مرحله {ToPersianNumber(index)} از {ToPersianNumber(total)}";

            LessonStatus =
                $"سطح: {ToPersianNumber(lesson.Level)} | " +
                $"نوع درس: {lesson.LessonType} | " +
                $"زمان تقریبی: {ToPersianNumber(lesson.EstimatedMinutes)} دقیقه | " +
                $"حد نصاب: {ToPersianNumber(lesson.PassingScore)}";

            var hasVideo = MediaPathService.Exists(lesson.VideoPath);
            var hasAudio = MediaPathService.Exists(lesson.AudioPath);
            var hasPdf = MediaPathService.Exists(lesson.PdfPath);

            if (!hasVideo && !hasAudio && !hasPdf)
            {
                MediaStatus = "برای این درس رسانه‌ای ثبت نشده یا فایل رسانه پیدا نشد.";
            }
            else
            {
                MediaStatus =
                    $"ویدئو: {(hasVideo ? "موجود" : "ندارد")} | " +
                    $"صوت: {(hasAudio ? "موجود" : "ندارد")} | " +
                    $"PDF: {(hasPdf ? "موجود" : "ندارد")}";
            }

            RaiseNavigationCommands();
        }
    }}


