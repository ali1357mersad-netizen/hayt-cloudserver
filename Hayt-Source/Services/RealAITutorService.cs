using Hayt.Licensing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public sealed class RealAITutorService : IRealAITutorService
{
    private readonly IAISettingsService _settingsService;
    private readonly HttpClient _httpClient;
    private readonly IPremiumAccessService _premiumAccess;

    public RealAITutorService()
        : this(
            new AISettingsService(),
            new HttpClient(),
            new PremiumAccessService())
    {
    }

    public RealAITutorService(IAISettingsService settingsService)
        : this(
            settingsService,
            new HttpClient(),
            new PremiumAccessService())
    {
    }

    public RealAITutorService(
        IAISettingsService settingsService,
        HttpClient httpClient)
        : this(
            settingsService,
            httpClient,
            new PremiumAccessService())
    {
    }

    public RealAITutorService(
        IAISettingsService settingsService,
        HttpClient httpClient,
        IPremiumAccessService premiumAccess)
    {
        _settingsService = settingsService ??
            throw new ArgumentNullException(nameof(settingsService));

        _httpClient = httpClient ??
            throw new ArgumentNullException(nameof(httpClient));

        _premiumAccess = premiumAccess ??
            throw new ArgumentNullException(nameof(premiumAccess));
    }

    public Task<AIRequestResult> AskAsync(
        string question,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return Task.FromResult(AIRequestResult.Success("لطفاً سؤال خود را بنویسید.", false));
        }

        string systemPrompt =
            "تو مربی هوشمند آموزشی برنامه حیات هستی. " +
            "پاسخ‌ها را فارسی، روشن، مرحله‌به‌مرحله و مناسب یادگیری بده. " +
            "اگر متن زمینه داده شد، اولویت را به همان متن بده. " +
            "اگر جواب قطعی نداری، با احتیاط توضیح بده و پیشنهاد مطالعه بده.";

        string userContent = string.IsNullOrWhiteSpace(context)
            ? question.Trim()
            : $"زمینه مطالعه:\n{context}\n\nسؤال کاربر:\n{question}";

        return CompleteWithLicenseGateAsync(
            new List<(string Role, string Content)>
            {
                ("system", systemPrompt),
                ("user", userContent)
            },
            PremiumFeature.RealAITutor,
            cancellationToken);
    }

    public Task<AIRequestResult> SummarizeAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(AIRequestResult.Success("متنی برای خلاصه‌سازی وجود ندارد.", false));
        }

        return CompleteWithLicenseGateAsync(
            new List<(string Role, string Content)>
            {
                ("system", "تو یک دستیار خلاصه‌ساز آموزشی فارسی هستی. متن را دقیق، کوتاه، کاربردی و bullet-point خلاصه کن."),
                ("user", $"این متن را خلاصه کن:\n\n{text}")
            },
            PremiumFeature.AISummarization,
            cancellationToken);
    }

    public Task<AIRequestResult> GenerateQuizAsync(
        string text,
        int questionCount = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(AIRequestResult.Success("متنی برای ساخت آزمون وجود ندارد.", false));
        }

        questionCount = Math.Clamp(questionCount, 1, 20);

        string prompt =
            $"از متن زیر {questionCount} سؤال چهارگزینه‌ای فارسی بساز. " +
            "برای هر سؤال ۴ گزینه بده و در پایان پاسخ صحیح را مشخص کن. " +
            "فرمت پیشنهادی:\n" +
            "سؤال ۱: ...\nالف) ...\nب) ...\nج) ...\nد) ...\nپاسخ صحیح: ...\n\n" +
            $"متن:\n{text}";

        return CompleteWithLicenseGateAsync(
            new List<(string Role, string Content)>
            {
                ("system", "تو تولیدکننده آزمون آموزشی هستی. سؤال‌ها باید دقیق، قابل فهم و مرتبط با متن باشند."),
                ("user", prompt)
            },
            PremiumFeature.AIQuizGeneration,
            cancellationToken);
    }

    public Task<AIRequestResult> CompleteAsync(
        IReadOnlyList<(string Role, string Content)> messages,
        CancellationToken cancellationToken = default)
    {
        return CompleteWithLicenseGateAsync(
            messages,
            PremiumFeature.RealAITutor,
            cancellationToken);
    }

    private async Task<AIRequestResult> CompleteWithLicenseGateAsync(
        IReadOnlyList<(string Role, string Content)> messages,
        PremiumFeature requiredFeature,
        CancellationToken cancellationToken = default)
    {
        if (messages is null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var settings = _settingsService.Load();

        // حالت محلی و آفلاین رایگان باقی می‌ماند.
        // وقتی مدل واقعی فعال نیست، هیچ Gate و هیچ HTTP اجرا نمی‌شود.
        if (!settings.CanUseRealAI)
        {
            return AIRequestResult.Success(
                BuildLocalFallback(
                    messages,
                    "تنظیمات مدل واقعی کامل نیست یا حالت واقعی غیرفعال است."),
                false);
        }

        // License Gate دقیقاً پیش از ورود به مسیر HTTP اجرا می‌شود.
        // اگر مجوز وجود نداشته باشد، اجرای متد همین‌جا متوقف می‌شود
        // و هیچ درخواست شبکه‌ای ارسال نخواهد شد.
        try
        {
            _premiumAccess.EnsureAccess(
                requiredFeature,
                forceRefresh: true);
        }
        catch (PremiumAccessDeniedException ex)
        {
            string fallback = settings.AllowOfflineFallback
                ? BuildLocalFallback(
                    messages,
                    "برای استفاده از مدل واقعی به لایسنس معتبر نیاز است. " +
                    ex.Message)
                : string.Empty;

            return AIRequestResult.Failure(
                ex.Message,
                fallback,
                403);
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                model = settings.Model,
                temperature = settings.Temperature,
                max_tokens = settings.MaxTokens,
                messages = messages
                    .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                    .Select(x => new
                    {
                        role = NormalizeRole(x.Role),
                        content = x.Content
                    })
                    .ToArray()
            };

            string json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, timeoutCts.Token)
                .ConfigureAwait(false);

            string responseText = await response.Content
                .ReadAsStringAsync(timeoutCts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string fallback = settings.AllowOfflineFallback
                    ? BuildLocalFallback(messages, $"خطای سرور AI: {(int)response.StatusCode}")
                    : string.Empty;

                return AIRequestResult.Failure(
                    $"خطا در ارتباط با مدل واقعی. StatusCode: {(int)response.StatusCode}",
                    fallback,
                    (int)response.StatusCode);
            }

            string content = ExtractContent(responseText);

            if (string.IsNullOrWhiteSpace(content))
            {
                string fallback = settings.AllowOfflineFallback
                    ? BuildLocalFallback(messages, "پاسخ مدل خالی بود.")
                    : string.Empty;

                return AIRequestResult.Failure(
                    "پاسخ مدل واقعی خالی یا نامعتبر بود.",
                    fallback,
                    (int)response.StatusCode);
            }

            return AIRequestResult.Success(content.Trim(), true);
        }
        catch (OperationCanceledException)
        {
            string fallback = settings.AllowOfflineFallback
                ? BuildLocalFallback(messages, "درخواست AI بیش از حد طول کشید یا لغو شد.")
                : string.Empty;

            return AIRequestResult.Failure(
                "زمان پاسخ‌دهی مدل واقعی تمام شد.",
                fallback);
        }
        catch (HttpRequestException ex)
        {
            string fallback = settings.AllowOfflineFallback
                ? BuildLocalFallback(messages, "اتصال اینترنت یا سرور AI در دسترس نیست.")
                : string.Empty;

            return AIRequestResult.Failure(
                "خطای شبکه در ارتباط با مدل واقعی: " + ex.Message,
                fallback);
        }
        catch (Exception ex)
        {
            string fallback = settings.AllowOfflineFallback
                ? BuildLocalFallback(messages, "خطای غیرمنتظره در مدل واقعی رخ داد.")
                : string.Empty;

            return AIRequestResult.Failure(
                "خطای غیرمنتظره در AI: " + ex.Message,
                fallback);
        }
    }

    private static string ExtractContent(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return string.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseText);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("choices", out JsonElement choices) &&
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0)
            {
                JsonElement first = choices[0];

                if (first.TryGetProperty("message", out JsonElement message) &&
                    message.TryGetProperty("content", out JsonElement content))
                {
                    return content.GetString() ?? string.Empty;
                }

                if (first.TryGetProperty("text", out JsonElement text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }

            if (root.TryGetProperty("content", out JsonElement directContent))
            {
                return directContent.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("output_text", out JsonElement outputText))
            {
                return outputText.GetString() ?? string.Empty;
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    private static string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return "user";
        }

        string normalized = role.Trim().ToLowerInvariant();

        return normalized switch
        {
            "system" => "system",
            "assistant" => "assistant",
            "user" => "user",
            _ => "user"
        };
    }

    private static string BuildLocalFallback(
        IReadOnlyList<(string Role, string Content)> messages,
        string reason)
    {
        string userText = messages
            .Where(x => string.Equals(NormalizeRole(x.Role), "user", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Content)
            .LastOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userText))
        {
            userText = messages.LastOrDefault().Content ?? string.Empty;
        }

        userText = userText.Trim();

        if (userText.Length > 700)
        {
            userText = userText[..700] + "...";
        }

        var builder = new StringBuilder();

        builder.AppendLine("پاسخ پشتیبان محلی:");
        builder.AppendLine();
        builder.AppendLine("در حال حاضر پاسخ از مدل واقعی دریافت نشد.");
        builder.AppendLine($"دلیل: {reason}");
        builder.AppendLine();
        builder.AppendLine("راهنمای سریع:");
        builder.AppendLine("1. متن یا سؤال را به بخش‌های کوچک‌تر تقسیم کن.");
        builder.AppendLine("2. نکات کلیدی را مشخص کن.");
        builder.AppendLine("3. اگر سؤال درسی است، ابتدا تعریف‌ها و مثال‌ها را مرور کن.");
        builder.AppendLine("4. بعد از اتصال اینترنت یا تنظیم API Key، دوباره امتحان کن.");

        if (!string.IsNullOrWhiteSpace(userText))
        {
            builder.AppendLine();
            builder.AppendLine("خلاصه ورودی شما:");
            builder.AppendLine(userText);
        }

        return builder.ToString().Trim();
    }
}


