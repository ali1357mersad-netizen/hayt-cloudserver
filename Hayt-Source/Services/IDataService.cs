using Hayt.Models;
using Hayt.Licensing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hayt.Services
{
    public interface IDataService
    {
        Task ImportSeedBooksAsync();

        Task ImportBookFromJsonAsync(string jsonFilePath);

        Task<List<Book>> GetBooksAsync();

        Task<List<Section>> GetSectionsAsync(int bookId);

        Task<List<Chapter>> GetChaptersAsync(int sectionId);

        Task<List<Lesson>> GetLessonsAsync(int chapterId);

        Task<List<Question>> GetQuestionsAsync(int lessonId);

        Task<Lesson?> GetLessonByIdAsync(int lessonId);

        Task SaveLessonProgressAsync(int lessonId, bool isCompleted, int score);

        Task<int> GetCompletedLessonCountAsync();

        Task<int> GetTotalScoreAsync();

        Task<int> GetTotalLessonCountAsync();

        Task<List<Category>> GetCategoriesAsync();

        Task<List<Book>> GetBooksByCategoryAsync(string categoryId);

        Task<int> GetLessonLastPositionAsync(int lessonId);

        Task SaveLessonLastPositionAsync(int lessonId, int lastPosition);

        Task<List<BookProgressReport>> GetBookProgressReportsAsync();
    }
}

