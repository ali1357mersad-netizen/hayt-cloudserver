using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hayt.Services
{
    public class JsonImportService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonImportService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            _jsonOptions.Converters.Add(new SingleOrArrayListConverterFactory());
        }

        public async Task ImportSeedBooksAsync()
        {
            var seedFolder = GetSeedFolder();

            if (!Directory.Exists(seedFolder))
            {
                return;
            }

            var jsonFiles = Directory.GetFiles(
                seedFolder,
                "*.json",
                SearchOption.TopDirectoryOnly);

            jsonFiles = Array.FindAll(
                jsonFiles,
                x => !string.Equals(
                    Path.GetFileName(x),
                    "categories.json",
                    StringComparison.OrdinalIgnoreCase));

            if (jsonFiles.Length == 0)
            {
                return;
            }

            Array.Sort(jsonFiles, StringComparer.CurrentCultureIgnoreCase);

            var report = new StringBuilder();

            report.AppendLine("گزارش Import کتاب‌ها");
            report.AppendLine($"زمان: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"مسیر: {seedFolder}");
            report.AppendLine(new string('=', 80));

            var successCount = 0;
            var failedCount = 0;

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    await ImportBookFromFileAsync(filePath);
                    successCount++;
                    report.AppendLine($"SUCCESS | {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    report.AppendLine($"FAILED  | {Path.GetFileName(filePath)}");
                    report.AppendLine($"MESSAGE | {ex.Message}");
                    report.AppendLine($"DETAILS | {ex}");
                    report.AppendLine(new string('-', 80));
                }
            }

            report.AppendLine(new string('=', 80));
            report.AppendLine($"کل کتاب‌ها: {jsonFiles.Length}");
            report.AppendLine($"موفق: {successCount}");
            report.AppendLine($"ناموفق: {failedCount}");

            var reportPath = Path.Combine(
                AppContext.BaseDirectory,
                "DataFiles",
                "json-import-report.txt");

            await File.WriteAllTextAsync(
                reportPath,
                report.ToString(),
                Encoding.UTF8);
        }

        public async Task ImportCategoriesAsync()
        {
            var seedFolder = GetSeedFolder();

            var categoriesPath = Path.Combine(
                seedFolder,
                "categories.json");

            if (!File.Exists(categoriesPath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(
                categoriesPath,
                Encoding.UTF8);

            var root = JsonSerializer.Deserialize<CategoriesRoot>(
                json,
                _jsonOptions);

            if (root?.Categories == null ||
                root.Categories.Count == 0)
            {
                return;
            }

            using var db = new AppDbContext();

            foreach (var category in root.Categories)
            {
                NormalizeCategory(category);

                var existing = await db.Categories.FindAsync(category.Id);

                var subCategoriesJson =
                    JsonSerializer.Serialize(
                        category.SubCategories,
                        _jsonOptions);

                if (existing == null)
                {
                    category.SubCategoriesJson = subCategoriesJson;
                    db.Categories.Add(category);
                }
                else
                {
                    existing.Title = category.Title;
                    existing.Icon = category.Icon;
                    existing.Color = category.Color;
                    existing.Description = category.Description;
                    existing.SubCategoriesJson = subCategoriesJson;
                }
            }

            await db.SaveChangesAsync();

            var reportPath = Path.Combine(
                AppContext.BaseDirectory,
                "DataFiles",
                "category-import-report.txt");

            var report =
                "گزارش Import دسته‌بندی‌ها" +
                Environment.NewLine +
                $"زمان: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" +
                Environment.NewLine +
                $"تعداد دسته‌ها: {root.Categories.Count}" +
                Environment.NewLine +
                "وضعیت: موفق";

            await File.WriteAllTextAsync(
                reportPath,
                report,
                Encoding.UTF8);
        }

        public async Task ImportBookFromFileAsync(string jsonFilePath)
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath))
            {
                throw new ArgumentException(
                    "مسیر فایل JSON خالی است.",
                    nameof(jsonFilePath));
            }

            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException(
                    "فایل JSON پیدا نشد.",
                    jsonFilePath);
            }

            if (string.Equals(
                Path.GetFileName(jsonFilePath),
                "categories.json",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(
                jsonFilePath,
                Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException(
                    $"فایل «{Path.GetFileName(jsonFilePath)}» خالی است.");
            }

            Book? book;

            try
            {
                book = JsonSerializer.Deserialize<Book>(
                    json,
                    _jsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    $"ساختار فایل «{Path.GetFileName(jsonFilePath)}» با مدل کتاب سازگار نیست. مسیر خطا: {ex.Path}",
                    ex);
            }

            if (book == null)
            {
                throw new InvalidDataException(
                    $"فایل «{Path.GetFileName(jsonFilePath)}» به یک کتاب معتبر تبدیل نشد.");
            }

            NormalizeBook(book, jsonFilePath);

            using var db = new AppDbContext();

            var existing = await db.Books
                .Include(x => x.Sections)
                    .ThenInclude(x => x.Chapters)
                        .ThenInclude(x => x.Lessons)
                            .ThenInclude(x => x.Questions)
                .FirstOrDefaultAsync(x => x.BookKey == book.BookKey);

            if (existing != null)
            {
                db.Books.Remove(existing);
                await db.SaveChangesAsync();
            }

            ResetDatabaseIds(book);

            db.Books.Add(book);

            await db.SaveChangesAsync();
        }

        private static string GetSeedFolder()
        {
            return Path.Combine(
                AppContext.BaseDirectory,
                "DataFiles",
                "SeedData");
        }

        private static void NormalizeCategory(Category category)
        {
            category.Id = category.Id?.Trim() ?? string.Empty;
            category.Title = category.Title?.Trim() ?? string.Empty;
            category.Icon = category.Icon?.Trim();
            category.Color = category.Color?.Trim();
            category.Description = category.Description?.Trim();

            category.SubCategories ??= new List<SubCategoryItem>();

            NormalizeSubCategories(category.SubCategories);
        }

        private static void NormalizeSubCategories(
            List<SubCategoryItem>? items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.Id = item.Id?.Trim() ?? string.Empty;
                item.Title = item.Title?.Trim() ?? string.Empty;
                item.Icon = item.Icon?.Trim();

                item.SubCategories ??= new List<SubCategoryItem>();

                NormalizeSubCategories(item.SubCategories);
            }
        }

        private static void NormalizeBook(
            Book book,
            string sourceFilePath)
        {
            book.BookKey = string.IsNullOrWhiteSpace(book.BookKey)
                ? Path.GetFileNameWithoutExtension(sourceFilePath)
                : book.BookKey.Trim();

            book.Title = string.IsNullOrWhiteSpace(book.Title)
                ? book.BookKey
                : book.Title.Trim();

            book.Subtitle = book.Subtitle?.Trim() ?? string.Empty;
            book.Author = book.Author?.Trim() ?? string.Empty;
            book.Description = book.Description?.Trim() ?? string.Empty;
            book.CoverImagePath =
                book.CoverImagePath?.Trim() ?? string.Empty;

            book.Language = string.IsNullOrWhiteSpace(book.Language)
                ? "fa"
                : book.Language.Trim();

            book.Version = string.IsNullOrWhiteSpace(book.Version)
                ? "1.0.0"
                : book.Version.Trim();

            book.Level = book.Level <= 0 ? 1 : book.Level;
            book.IsActive = true;

            book.CategoryId = string.IsNullOrWhiteSpace(book.CategoryId)
                ? "uncategorized"
                : book.CategoryId.Trim();

            book.CategoryTitle = string.IsNullOrWhiteSpace(book.CategoryTitle)
                ? "دسته‌بندی‌نشده"
                : book.CategoryTitle.Trim();

            book.Sections ??= new List<Section>();

            for (var s = 0; s < book.Sections.Count; s++)
            {
                var section = book.Sections[s];

                section.SectionKey =
                    string.IsNullOrWhiteSpace(section.SectionKey)
                        ? $"{book.BookKey}-S{s + 1}"
                        : section.SectionKey.Trim();

                section.Title =
                    string.IsNullOrWhiteSpace(section.Title)
                        ? $"بخش {s + 1}"
                        : section.Title.Trim();

                section.OrderNumber =
                    section.OrderNumber <= 0 ? s + 1 : section.OrderNumber;

                section.Book = null;
                section.Chapters ??= new List<Chapter>();

                for (var c = 0; c < section.Chapters.Count; c++)
                {
                    var chapter = section.Chapters[c];

                    chapter.ChapterKey =
                        string.IsNullOrWhiteSpace(chapter.ChapterKey)
                            ? $"{section.SectionKey}-C{c + 1}"
                            : chapter.ChapterKey.Trim();

                    chapter.Title =
                        string.IsNullOrWhiteSpace(chapter.Title)
                            ? $"فصل {c + 1}"
                            : chapter.Title.Trim();

                    chapter.OrderNumber =
                        chapter.OrderNumber <= 0 ? c + 1 : chapter.OrderNumber;

                    chapter.Section = null;
                    chapter.Lessons ??= new List<Lesson>();

                    for (var l = 0; l < chapter.Lessons.Count; l++)
                    {
                        var lesson = chapter.Lessons[l];

                        lesson.LessonKey =
                            string.IsNullOrWhiteSpace(lesson.LessonKey)
                                ? $"{chapter.ChapterKey}-L{l + 1}"
                                : lesson.LessonKey.Trim();

                        lesson.Title =
                            string.IsNullOrWhiteSpace(lesson.Title)
                                ? $"درس {l + 1}"
                                : lesson.Title.Trim();

                        lesson.Content =
                            lesson.Content?.Trim() ?? string.Empty;

                        lesson.VideoPath =
                            lesson.VideoPath?.Trim() ?? string.Empty;

                        lesson.AudioPath =
                            lesson.AudioPath?.Trim() ?? string.Empty;

                        lesson.PdfPath =
                            lesson.PdfPath?.Trim() ?? string.Empty;

                        lesson.Tags =
                            lesson.Tags?.Trim() ?? string.Empty;

                        lesson.Level =
                            lesson.Level <= 0 ? 1 : lesson.Level;

                        lesson.OrderNumber =
                            lesson.OrderNumber <= 0 ? l + 1 : lesson.OrderNumber;

                        lesson.EstimatedMinutes =
                            lesson.EstimatedMinutes <= 0 ? 10 : lesson.EstimatedMinutes;

                        lesson.LessonType =
                            string.IsNullOrWhiteSpace(lesson.LessonType)
                                ? "Educational"
                                : lesson.LessonType.Trim();

                        lesson.PassingScore =
                            lesson.PassingScore <= 0 ? 70 : lesson.PassingScore;

                        lesson.DefaultPlaybackSpeed =
                            lesson.DefaultPlaybackSpeed <= 0
                                ? 1.0
                                : lesson.DefaultPlaybackSpeed;

                        lesson.IsActive = true;
                        lesson.AllowDownload = true;
                        lesson.Chapter = null;
                        lesson.Questions ??= new List<Question>();

                        for (var q = 0;
                             q < lesson.Questions.Count;
                             q++)
                        {
                            var question = lesson.Questions[q];

                            question.QuestionText =
                                question.QuestionText?.Trim()
                                ?? string.Empty;

                            question.OptionA =
                                question.OptionA?.Trim()
                                ?? string.Empty;

                            question.OptionB =
                                question.OptionB?.Trim()
                                ?? string.Empty;

                            question.OptionC =
                                question.OptionC?.Trim()
                                ?? string.Empty;

                            question.OptionD =
                                question.OptionD?.Trim()
                                ?? string.Empty;

                            question.Explanation =
                                question.Explanation?.Trim()
                                ?? string.Empty;

                            question.OrderNumber =
                                question.OrderNumber <= 0
                                    ? q + 1
                                    : question.OrderNumber;

                            question.Lesson = null;
                        }
                    }
                }
            }

            book.CreatedAt =
                book.CreatedAt == default
                    ? DateTime.Now
                    : book.CreatedAt;

            book.UpdatedAt = DateTime.Now;
        }

        private static void ResetDatabaseIds(Book book)
        {
            book.Id = 0;

            foreach (var section in book.Sections)
            {
                section.Id = 0;
                section.BookId = 0;

                foreach (var chapter in section.Chapters)
                {
                    chapter.Id = 0;
                    chapter.SectionId = 0;

                    foreach (var lesson in chapter.Lessons)
                    {
                        lesson.Id = 0;
                        lesson.ChapterId = 0;

                        foreach (var question in lesson.Questions)
                        {
                            question.Id = 0;
                            question.LessonId = 0;
                        }
                    }
                }
            }
        }

        private sealed class CategoriesRoot
        {
            public List<Category> Categories { get; set; } = new();
        }
    }

    internal sealed class SingleOrArrayListConverterFactory
        : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType &&
                   typeToConvert.GetGenericTypeDefinition()
                       == typeof(List<>);
        }

        public override JsonConverter CreateConverter(
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var elementType =
                typeToConvert.GetGenericArguments()[0];

            var converterType =
                typeof(SingleOrArrayListConverter<>)
                    .MakeGenericType(elementType);

            return (JsonConverter)Activator.CreateInstance(
                converterType)!;
        }
    }

    internal sealed class SingleOrArrayListConverter<T>
        : JsonConverter<List<T>>
    {
        public override List<T> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var document =
                JsonDocument.ParseValue(ref reader);

            var root = document.RootElement;
            var result = new List<T>();

            if (root.ValueKind == JsonValueKind.Null)
            {
                return result;
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var value = JsonSerializer.Deserialize<T>(
                        item.GetRawText(),
                        options);

                    if (value != null)
                    {
                        result.Add(value);
                    }
                }

                return result;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                var value = JsonSerializer.Deserialize<T>(
                    root.GetRawText(),
                    options);

                if (value != null)
                {
                    result.Add(value);
                }

                return result;
            }

            throw new JsonException(
                $"برای List<{typeof(T).Name}> باید آرایه یا شیء JSON ارائه شود.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            List<T> value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var item in value)
            {
                JsonSerializer.Serialize(
                    writer,
                    item,
                    options);
            }

            writer.WriteEndArray();
        }
    }
}


