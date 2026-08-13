using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hayt.Services;
using Hayt.Licensing.Services;
namespace Hayt.Services
{
    public class SqliteDataService : IDataService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SqliteDataService(AppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task ImportSeedBooksAsync()
        {
            var hasCompleteBook = await _context.Books
                .AnyAsync(b => b.Sections.Any(s =>
                    s.Chapters.Any(c => c.Lessons.Any())));

            if (hasCompleteBook)
                return;

            var seedFolder = Path.Combine(AppContext.BaseDirectory, "DataFiles", "SeedData");
            if (!Directory.Exists(seedFolder))
                return;

            var jsonFiles = Directory.GetFiles(seedFolder, "*.json");
            if (jsonFiles.Length == 0)
                return;

            var importer = new JsonImportService();
            foreach (var file in jsonFiles)
            {
                await importer.ImportBookFromFileAsync(file);
            }
        }

        public async Task ImportBookFromJsonAsync(string jsonFilePath)
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath) || !File.Exists(jsonFilePath))
                return;

            var json = await File.ReadAllTextAsync(jsonFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var books = JsonSerializer.Deserialize<List<Book>>(json, options);

            if (books == null || books.Count == 0)
                return;

            _context.Books.AddRange(books);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Book>> GetBooksAsync()
        {
            return await _context.Books
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Level)
                .ThenBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<List<Section>> GetSectionsAsync(int bookId)
        {
            return await _context.Sections
                .AsNoTracking()
                .Where(s => s.BookId == bookId)
                .OrderBy(s => s.OrderNumber)
                .ToListAsync();
        }

        public async Task<List<Chapter>> GetChaptersAsync(int sectionId)
        {
            return await _context.Chapters
                .AsNoTracking()
                .Where(c => c.SectionId == sectionId)
                .OrderBy(c => c.OrderNumber)
                .ToListAsync();
        }

        public async Task<List<Lesson>> GetLessonsAsync(int chapterId)
        {
            return await _context.Lessons
                .AsNoTracking()
                .Where(l => l.ChapterId == chapterId && l.IsActive)
                .OrderBy(l => l.OrderNumber)
                .ToListAsync();
        }

        public async Task<Lesson?> GetLessonByIdAsync(int lessonId)
        {
            if (lessonId <= 0)
            {
                return null;
            }

            return await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId && l.IsActive);
        }

        public async Task<List<Question>> GetQuestionsAsync(int lessonId)
        {
            return await _context.Questions
                .AsNoTracking()
                .Where(q => q.LessonId == lessonId)
                .OrderBy(q => q.OrderNumber)
                .ToListAsync();
        }

        public async Task SaveLessonProgressAsync(int lessonId, bool isCompleted, int score)
        {
            if (lessonId <= 0)
            {
                return;
            }

            score = Math.Clamp(score, 0, 100);

            var now = DateTime.UtcNow;

            var progress = await _context.UserProgresses
                .FirstOrDefaultAsync(p =>
                    p.UserId == _currentUserService.CurrentUserId &&
                    p.LessonId == lessonId);

            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = _currentUserService.CurrentUserId,
                    LessonId = lessonId,
                    IsCompleted = isCompleted,
                    Score = score,
                    UpdatedAt = now,
                    CompletedAt = isCompleted ? now : null
                };

                _context.UserProgresses.Add(progress);
            }
            else
            {
                if (score > progress.Score)
                {
                    progress.Score = score;
                }

                if (isCompleted && !progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = now;
                }
                else if (isCompleted && progress.IsCompleted && progress.CompletedAt == null)
                {
                    progress.CompletedAt = now;
                }

                progress.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCompletedLessonCountAsync()
        {
            return await _context.UserProgresses
                .AsNoTracking()
                .CountAsync(p => p.UserId == _currentUserService.CurrentUserId && p.IsCompleted);
        }

        public async Task<int> GetTotalScoreAsync()
        {
            return await _context.UserProgresses
                .AsNoTracking()
                .Where(p => p.UserId == _currentUserService.CurrentUserId && p.IsCompleted)
                .SumAsync(p => p.Score);
        }

        public async Task<int> GetTotalLessonCountAsync()
        {
            return await _context.Lessons
                .AsNoTracking()
                .CountAsync(l => l.IsActive);
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<List<Book>> GetBooksByCategoryAsync(string categoryId)
        {
            return await _context.Books
                .AsNoTracking()
                .Where(b => b.IsActive && b.CategoryId == categoryId)
                .OrderBy(b => b.Level)
                .ThenBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<int> GetLessonLastPositionAsync(int lessonId)
        {
            var progress = await _context.UserProgresses
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.UserId == _currentUserService.CurrentUserId &&
                    p.LessonId == lessonId);

            return progress?.LastPosition ?? 0;
        }

        public async Task SaveLessonLastPositionAsync(int lessonId, int lastPosition)
        {
            if (lessonId <= 0)
            {
                return;
            }

            lastPosition = Math.Max(0, lastPosition);

            var progress = await _context.UserProgresses
                .FirstOrDefaultAsync(p =>
                    p.UserId == _currentUserService.CurrentUserId &&
                    p.LessonId == lessonId);

            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = _currentUserService.CurrentUserId,
                    LessonId = lessonId,
                    IsCompleted = false,
                    Score = 0
                };

                _context.UserProgresses.Add(progress);
            }

            progress.LastPosition = lastPosition;
            progress.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<BookProgressReport>> GetBookProgressReportsAsync()
        {
            var books = await _context.Books
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Include(b => b.Sections)
                    .ThenInclude(s => s.Chapters)
                        .ThenInclude(c => c.Lessons)
                .OrderBy(b => b.Level)
                .ThenBy(b => b.Title)
                .ToListAsync();

            var completedProgresses = await _context.UserProgresses
                .AsNoTracking()
                .Where(p => p.UserId == _currentUserService.CurrentUserId && p.IsCompleted)
                .ToListAsync();

            var completedLessonIds = completedProgresses
                .Select(p => p.LessonId)
                .ToHashSet();

            var reports = new List<BookProgressReport>();

            foreach (var book in books)
            {
                var lessons = book.Sections
                    .OrderBy(s => s.OrderNumber)
                    .SelectMany(s => s.Chapters.OrderBy(c => c.OrderNumber))
                    .SelectMany(c => c.Lessons.OrderBy(l => l.OrderNumber))
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.OrderNumber)
                    .ThenBy(l => l.Id)
                    .ToList();

                var totalLessons = lessons.Count;

                var completedLessons = lessons
                    .Where(l => completedLessonIds.Contains(l.Id))
                    .ToList();

                var completedLessonCount = completedLessons.Count;

                var percent = totalLessons == 0
                    ? 0
                    : completedLessonCount * 100.0 / totalLessons;

                var completedMinutes = completedLessons.Sum(l =>
                    l.EstimatedMinutes > 0 ? l.EstimatedMinutes : 90);

                var completedHours = completedMinutes / 60.0;

                var lessonIds = lessons
                    .Select(l => l.Id)
                    .ToHashSet();

                var earnedXp = completedProgresses
                    .Where(p => lessonIds.Contains(p.LessonId))
                    .Sum(p => p.Score);

                if (totalLessons > 0 && completedLessonCount == totalLessons)
                {
                    earnedXp += 1000;
                }

                var completedTitles = completedLessons
                    .Select(l => l.Title)
                    .ToList();

                var remainingLessons = lessons
                    .Where(l => !completedLessonIds.Contains(l.Id))
                    .ToList();

                var remainingTitles = remainingLessons
                    .Select(l => l.Title)
                    .ToList();

                var remainingLessonItems = remainingLessons
                    .Select(l => new LessonNavigationItem
                    {
                        LessonId = l.Id,
                        Title = l.Title,
                        OrderNumber = l.OrderNumber
                    })
                    .ToList();

                reports.Add(new BookProgressReport
                {
                    BookId = book.Id,
                    BookTitle = book.Title,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessonCount,
                    CompletionPercent = percent,
                    CompletedHours = completedHours,
                    EarnedXp = earnedXp,
                    IsCompleted = totalLessons > 0 && completedLessonCount == totalLessons,
                    CompletedLessonTitles = completedTitles,
                    RemainingLessonTitles = remainingTitles,
                    RemainingLessonItems = remainingLessonItems
                });
            }

            return reports;
        }
    }
}

