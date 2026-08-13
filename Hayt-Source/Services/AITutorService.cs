using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class AITutorService : IAITutorService
{
    private readonly IStudyNotesService _notesService;

    public AITutorSession CurrentSession { get; private set; } =
        AITutorSession.CreateGeneral();

    public event EventHandler? SessionChanged;

    public AITutorService(IStudyNotesService notesService)
    {
        _notesService = notesService ??
            throw new ArgumentNullException(nameof(notesService));
    }

    public AITutorSession StartGeneralSession()
    {
        CurrentSession = AITutorSession.CreateGeneral();
        AddSystemMessage("سلام! من مربی هوشمند حیات هستم. درباره هر موضوعی بپرس.");
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return CurrentSession;
    }

    public AITutorSession StartBookSession(string bookId, string? bookTitle = null)
    {
        CurrentSession = AITutorSession.CreateForBook(bookId, bookTitle);
        AddSystemMessage($"سلام! من مربی هوشمند حیات هستم. درباره کتاب «{bookTitle ?? bookId}» بپرس.");
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return CurrentSession;
    }

    public AITutorSession StartLessonSession(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        CurrentSession = AITutorSession.CreateForLesson(bookId, bookTitle, lessonId, lessonTitle);
        AddSystemMessage($"سلام! من مربی هوشمند حیات هستم. درباره درس «{lessonTitle ?? lessonId}» بپرس.");
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return CurrentSession;
    }

    public async Task<AITutorMessage> AskAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("سوال نمی‌تواند خالی باشد.", nameof(question));
        }

        var userMessage = new AITutorMessage
        {
            Role = AITutorRole.User,
            Content = question.Trim()
        };

        CurrentSession.Messages.Add(userMessage);
        CurrentSession.LastActivityAt = DateTime.Now;

        await Task.Delay(300);

        var response = BuildResponse(question.Trim());
        CurrentSession.Messages.Add(response);
        CurrentSession.LastActivityAt = DateTime.Now;

        SessionChanged?.Invoke(this, EventArgs.Empty);
        return response;
    }

    public async Task<AITutorMessage> SummarizeNotesAsync(IReadOnlyList<StudyNote> notes)
    {
        if (notes is null || notes.Count == 0)
        {
            return await Task.FromResult(CreateAssistantMessage(
                "هنوز یادداشتی برای خلاصه‌سازی وجود ندارد. ابتدا چند یادداشت بنویسید."));
        }

        var userMessage = new AITutorMessage
        {
            Role = AITutorRole.User,
            Content = $"لطفاً {notes.Count} یادداشت زیر را خلاصه کن."
        };

        CurrentSession.Messages.Add(userMessage);
        CurrentSession.LastActivityAt = DateTime.Now;

        await Task.Delay(400);

        var summary = BuildNotesSummary(notes);
        var response = CreateAssistantMessage(summary);

        CurrentSession.Messages.Add(response);
        CurrentSession.LastActivityAt = DateTime.Now;

        SessionChanged?.Invoke(this, EventArgs.Empty);
        return response;
    }

    public async Task<AITutorMessage> ExplainTopicAsync(string topic, string? context = null)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("موضوع نمی‌تواند خالی باشد.", nameof(topic));
        }

        var userMessage = new AITutorMessage
        {
            Role = AITutorRole.User,
            Content = $"لطفاً موضوع «{topic.Trim()}» را توضیح بده."
        };

        CurrentSession.Messages.Add(userMessage);
        CurrentSession.LastActivityAt = DateTime.Now;

        await Task.Delay(300);

        var response = CreateAssistantMessage(
            BuildTopicExplanation(topic.Trim(), context));

        CurrentSession.Messages.Add(response);
        CurrentSession.LastActivityAt = DateTime.Now;

        SessionChanged?.Invoke(this, EventArgs.Empty);
        return response;
    }

    public async Task<AITutorMessage> GenerateQuizAsync(
        string topic,
        int questionCount = 5,
        string? context = null)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("موضوع نمی‌تواند خالی باشد.", nameof(topic));
        }

        questionCount = Math.Clamp(questionCount, 3, 10);

        var userMessage = new AITutorMessage
        {
            Role = AITutorRole.User,
            Content = $"برای موضوع «{topic.Trim()}» {questionCount} سوال چهارگزینه‌ای بساز."
        };

        CurrentSession.Messages.Add(userMessage);
        CurrentSession.LastActivityAt = DateTime.Now;

        await Task.Delay(500);

        var response = CreateAssistantMessage(
            BuildQuiz(topic.Trim(), questionCount));

        CurrentSession.Messages.Add(response);
        CurrentSession.LastActivityAt = DateTime.Now;

        SessionChanged?.Invoke(this, EventArgs.Empty);
        return response;
    }

    public void ClearSession()
    {
        CurrentSession.Messages.Clear();
        CurrentSession.LastActivityAt = DateTime.Now;
        AddSystemMessage("گفتگو پاک شد. سوال جدیدی بپرس.");
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddSystemMessage(string content)
    {
        CurrentSession.Messages.Add(new AITutorMessage
        {
            Role = AITutorRole.System,
            Content = content
        });
    }

    private AITutorMessage CreateAssistantMessage(string content)
    {
        return new AITutorMessage
        {
            Role = AITutorRole.Assistant,
            Content = content
        };
    }

    private AITutorMessage BuildResponse(string question)
    {
        string lower = question.ToLowerInvariant();

        if (ContainsAny(lower, "سلام", "درود", "hi", "hello", "salam"))
        {
            return CreateAssistantMessage(
                "سلام! 👋 من مربی هوشمند حیات هستم. خوشحالم که اینجایی.\n\n" +
                "می‌توانم این کارها را برایت انجام دهم:\n" +
                "• توضیح موضوعات درسی\n" +
                "• خلاصه‌سازی یادداشت‌ها\n" +
                "• ساخت سوال چهارگزینه‌ای\n" +
                "• پاسخ به سوالات یادگیری\n\n" +
                "بگو چه کمکی می‌خواهی!");
        }

        if (ContainsAny(lower, "خلاصه", "summary", "خلاصه کن"))
        {
            var notes = _notesService.GetAll();
            return CreateAssistantMessage(BuildNotesSummary(notes));
        }

        if (ContainsAny(lower, "سوال", "آزمون", "quiz", "تست", "چهارگزینه"))
        {
            return CreateAssistantMessage(BuildQuiz("موضوع انتخابی", 5));
        }

        if (ContainsAny(lower, "یادداشت", "note", "چند یادداشت"))
        {
            var notes = _notesService.GetAll();
            int count = notes.Count;
            int important = notes.Count(x => x.IsImportant);

            return CreateAssistantMessage(
                $"در حال حاضر {count} یادداشت داری.\n" +
                $"از این تعداد {important} یادداشت مهم است.\n\n" +
                "می‌توانی از من بخواهی آن‌ها را خلاصه کنم یا سوال از آن‌ها بسازم.");
        }

        if (ContainsAny(lower, "کتاب", "درس", "book", "lesson"))
        {
            return CreateAssistantMessage(
                "برای دریافت کمک دقیق‌تر درباره یک کتاب یا درس:\n\n" +
                "۱. از داشبورد، کتاب یا درس موردنظر را انتخاب کن.\n" +
                "۲. دکمه «مربی هوشمند» را بزن.\n" +
                "۳. سوال خود را بپرس.\n\n" +
                "من بر اساس محتوای همان کتاب/درس پاسخ می‌دهم.");
        }

        if (ContainsAny(lower, "ممنون", "تشکر", "thanks", "thank"))
        {
            return CreateAssistantMessage(
                "خواهش می‌کنم! 😊 هر وقت سوالی داشتی من اینجام.");
        }

        if (ContainsAny(lower, "خداحافظ", "bye", "خدافظ"))
        {
            return CreateAssistantMessage(
                "خداحافظ! موفق باشی. 🌟 هر وقت برگشتی من اینجام.");
        }

        return CreateAssistantMessage(
            $"سوال خوبی پرسیدی! 🤔\n\n" +
            $"«{question}»\n\n" +
            "برای پاسخ دقیق‌تر، می‌توانی:\n" +
            "• موضوع را مشخص‌تر بپرسی (مثلاً: «فلان مبحث را توضیح بده»)\n" +
            "• از من بخواهی یادداشت‌هایت را خلاصه کنم\n" +
            "• از من بخواهی سوال چهارگزینه‌ای بسازم\n\n" +
            "این نسخه اولیه مربی است و به‌زودی به مدل هوش مصنوعی پیشرفته متصل می‌شود.");
    }

    private static string BuildNotesSummary(IReadOnlyList<StudyNote> notes)
    {
        if (notes is null || notes.Count == 0)
        {
            return "هنوز یادداشتی برای خلاصه‌سازی وجود ندارد.";
        }

        var lines = new List<string>
        {
            $"📊 خلاصه {notes.Count} یادداشت:",
            ""
        };

        int importantCount = notes.Count(x => x.IsImportant);
        int pinnedCount = notes.Count(x => x.IsPinned);

        lines.Add($"• {notes.Count} یادداشت کل");
        lines.Add($"• {importantCount} یادداشت مهم");
        lines.Add($"• {pinnedCount} یادداشت سنجاق‌شده");
        lines.Add("");

        var recent = notes
            .OrderByDescending(x => x.UpdatedAt)
            .Take(5)
            .ToArray();

        if (recent.Length > 0)
        {
            lines.Add("🔝 جدیدترین یادداشت‌ها:");
            foreach (var note in recent)
            {
                lines.Add($"  • {note.DisplayTitle} — {note.ShortContent}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildTopicExplanation(string topic, string? context)
    {
        string contextLine = string.IsNullOrWhiteSpace(context)
            ? ""
            : $" (در چارچوب: {context})";

        return
            $"📚 توضیح موضوع «{topic}»{contextLine}:\n\n" +
            "این نسخه اولیه مربی هوشمند است و پاسخ‌های آن بر اساس الگوهای آماده تولید می‌شود.\n\n" +
            "برای دریافت توضیح دقیق و هوشمند، این مراحل را انجام بده:\n\n" +
            "۱. از داشبورد، کتاب یا درس موردنظر را انتخاب کن.\n" +
            "۲. دکمه «مربی هوشمند» را بزن.\n" +
            "۳. سوال خود را در همان چارچوب بپرس.\n\n" +
            "در نسخه‌های بعدی، مربی به مدل هوش مصنوعی پیشرفته متصل می‌شود و پاسخ‌های دقیق‌تری می‌دهد.";
    }

    private static string BuildQuiz(string topic, int questionCount)
    {
        var lines = new List<string>
        {
            $"📝 آزمون چهارگزینه‌ای — موضوع: {topic}",
            $"تعداد سوالات: {questionCount}",
            ""
        };

        for (int i = 1; i <= questionCount; i++)
        {
            lines.Add($"سوال {i}:");
            lines.Add("  الف) گزینه اول");
            lines.Add("  ب) گزینه دوم");
            lines.Add("  ج) گزینه سوم");
            lines.Add("  د) گزینه چهارم");
            lines.Add("");
        }

        lines.Add("این سوالات نمونه هستند. در نسخه کامل، سوالات بر اساس محتوای واقعی کتاب/درس ساخته می‌شوند.");

        return string.Join(Environment.NewLine, lines);
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> ExecuteRealAIAsync(
        string toolName,
        string input,
        string? context = null)
    {
        IAITutorBridgeService bridge = new AITutorBridgeService();
        AITutorBridgeResponse response = await bridge.ExecuteToolAsync(toolName, input, context);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            return response.Content;
        }

        return string.IsNullOrWhiteSpace(response.ErrorMessage)
            ? "پاسخی از هوش مصنوعی دریافت نشد."
            : "خطا در هوش مصنوعی: " + response.ErrorMessage;
    }
}

