using System;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class AITutorBridgeService : IAITutorBridgeService
{
    private readonly IRealAITutorService _realAI;

    public AITutorBridgeService()
        : this(new RealAITutorService())
    {
    }

    public AITutorBridgeService(IRealAITutorService realAI)
    {
        _realAI = realAI ??
            throw new ArgumentNullException(nameof(realAI));
    }

    public async Task<AITutorBridgeResponse> AskAsync(
        string question,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AIRequestResult result = await _realAI
                .AskAsync(question, context, cancellationToken)
                .ConfigureAwait(false);

            return AITutorBridgeResponse.FromAIResult(AITutorRequestKind.Ask, result);
        }
        catch (Exception ex)
        {
            return AITutorBridgeResponse.Failure(
                AITutorRequestKind.Ask,
                ex.Message);
        }
    }

    public async Task<AITutorBridgeResponse> SummarizeNotesAsync(
        string notesText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AIRequestResult result = await _realAI
                .SummarizeAsync(notesText, cancellationToken)
                .ConfigureAwait(false);

            return AITutorBridgeResponse.FromAIResult(AITutorRequestKind.Summarize, result);
        }
        catch (Exception ex)
        {
            return AITutorBridgeResponse.Failure(
                AITutorRequestKind.Summarize,
                ex.Message);
        }
    }

    public async Task<AITutorBridgeResponse> GenerateQuizAsync(
        string sourceText,
        int questionCount = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AIRequestResult result = await _realAI
                .GenerateQuizAsync(sourceText, questionCount, cancellationToken)
                .ConfigureAwait(false);

            return AITutorBridgeResponse.FromAIResult(AITutorRequestKind.Quiz, result);
        }
        catch (Exception ex)
        {
            return AITutorBridgeResponse.Failure(
                AITutorRequestKind.Quiz,
                ex.Message);
        }
    }

    public async Task<AITutorBridgeResponse> ExecuteToolAsync(
        string toolName,
        string input,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        AITutorRequestKind kind = DetectKind(toolName);

        return kind switch
        {
            AITutorRequestKind.Summarize =>
                await SummarizeNotesAsync(input, cancellationToken).ConfigureAwait(false),

            AITutorRequestKind.Quiz =>
                await GenerateQuizAsync(input, 5, cancellationToken).ConfigureAwait(false),

            AITutorRequestKind.Explain =>
                await AskAsync(
                    "این موضوع را ساده، آموزشی و مرحله‌به‌مرحله توضیح بده:" +
                    Environment.NewLine +
                    input,
                    context,
                    cancellationToken).ConfigureAwait(false),

            AITutorRequestKind.StudyPlan =>
                await AskAsync(
                    "برای مطالعه این موضوع یک برنامه کوتاه، عملی و قابل اجرا بده:" +
                    Environment.NewLine +
                    input,
                    context,
                    cancellationToken).ConfigureAwait(false),

            AITutorRequestKind.Ask =>
                await AskAsync(input, context, cancellationToken).ConfigureAwait(false),

            _ =>
                await AskAsync(input, context, cancellationToken).ConfigureAwait(false)
        };
    }

    public AITutorRequestKind DetectKind(string? toolNameOrCommand)
    {
        if (string.IsNullOrWhiteSpace(toolNameOrCommand))
        {
            return AITutorRequestKind.Ask;
        }

        string value = toolNameOrCommand.Trim().ToLowerInvariant();

        if (value.Contains("summary") ||
            value.Contains("summarize") ||
            value.Contains("summarise") ||
            value.Contains("خلاصه") ||
            value.Contains("خلاصه‌سازی"))
        {
            return AITutorRequestKind.Summarize;
        }

        if (value.Contains("quiz") ||
            value.Contains("test") ||
            value.Contains("mcq") ||
            value.Contains("question") ||
            value.Contains("آزمون") ||
            value.Contains("سوال") ||
            value.Contains("سؤال") ||
            value.Contains("چهارگزینه"))
        {
            return AITutorRequestKind.Quiz;
        }

        if (value.Contains("explain") ||
            value.Contains("شرح") ||
            value.Contains("توضیح"))
        {
            return AITutorRequestKind.Explain;
        }

        if (value.Contains("plan") ||
            value.Contains("schedule") ||
            value.Contains("برنامه") ||
            value.Contains("مطالعه"))
        {
            return AITutorRequestKind.StudyPlan;
        }

        if (value.Contains("ask") ||
            value.Contains("chat") ||
            value.Contains("پرسش") ||
            value.Contains("سؤال") ||
            value.Contains("سوال"))
        {
            return AITutorRequestKind.Ask;
        }

        return AITutorRequestKind.Ask;
    }
}

