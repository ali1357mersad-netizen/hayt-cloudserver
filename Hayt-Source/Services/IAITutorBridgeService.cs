using System.Threading;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface IAITutorBridgeService
{
    Task<AITutorBridgeResponse> AskAsync(
        string question,
        string? context = null,
        CancellationToken cancellationToken = default);

    Task<AITutorBridgeResponse> SummarizeNotesAsync(
        string notesText,
        CancellationToken cancellationToken = default);

    Task<AITutorBridgeResponse> GenerateQuizAsync(
        string sourceText,
        int questionCount = 5,
        CancellationToken cancellationToken = default);

    Task<AITutorBridgeResponse> ExecuteToolAsync(
        string toolName,
        string input,
        string? context = null,
        CancellationToken cancellationToken = default);

    AITutorRequestKind DetectKind(string? toolNameOrCommand);
}

