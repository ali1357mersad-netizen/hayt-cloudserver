using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IRealAITutorService
{
    Task<AIRequestResult> AskAsync(
        string question,
        string? context = null,
        CancellationToken cancellationToken = default);

    Task<AIRequestResult> SummarizeAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<AIRequestResult> GenerateQuizAsync(
        string text,
        int questionCount = 5,
        CancellationToken cancellationToken = default);

    Task<AIRequestResult> CompleteAsync(
        IReadOnlyList<(string Role, string Content)> messages,
        CancellationToken cancellationToken = default);
}

