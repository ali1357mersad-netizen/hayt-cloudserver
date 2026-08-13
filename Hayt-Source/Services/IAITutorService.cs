using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IAITutorService
{
    AITutorSession CurrentSession { get; }

    event EventHandler? SessionChanged;

    AITutorSession StartGeneralSession();

    AITutorSession StartBookSession(string bookId, string? bookTitle = null);

    AITutorSession StartLessonSession(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null);

    Task<AITutorMessage> AskAsync(string question);

    Task<AITutorMessage> SummarizeNotesAsync(IReadOnlyList<StudyNote> notes);

    Task<AITutorMessage> ExplainTopicAsync(string topic, string? context = null);

    Task<AITutorMessage> GenerateQuizAsync(
        string topic,
        int questionCount = 5,
        string? context = null);

    void ClearSession();
}

