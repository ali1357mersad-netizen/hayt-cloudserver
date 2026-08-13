using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class AITutorViewModel : ObservableObject
{
    private readonly IAITutorService _tutorService;

    [ObservableProperty]
    private AITutorSession _session;

    [ObservableProperty]
    private string _questionText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public IReadOnlyList<AITutorMessage> Messages =>
        Session.RecentMessages(50);

    public string SessionTitle =>
        $"مربی هوشمند — {Session.ContextTitle}";

    public string SessionContextText =>
        Session.HasContext
            ? Session.ContextTitle
            : "گفتگوی عمومی";

    public bool HasMessages =>
        Messages.Count > 0;

    public bool CanAsk =>
        !IsBusy && !string.IsNullOrWhiteSpace(QuestionText);

    public AITutorViewModel(IAITutorService tutorService)
    {
        _tutorService = tutorService ??
            throw new ArgumentNullException(nameof(tutorService));

        Session = _tutorService.CurrentSession;

        _tutorService.SessionChanged += (_, _) =>
        {
            Session = _tutorService.CurrentSession;
            RaiseState();
        };

        RaiseState();
    }

    public void StartGeneral()
    {
        _tutorService.StartGeneralSession();
        RaiseState();
    }

    public void StartForBook(string bookId, string? bookTitle = null)
    {
        _tutorService.StartBookSession(bookId, bookTitle);
        RaiseState();
    }

    public void StartForLesson(
        string? bookId,
        string? bookTitle,
        string lessonId,
        string? lessonTitle = null)
    {
        _tutorService.StartLessonSession(bookId, bookTitle, lessonId, lessonTitle);
        RaiseState();
    }

    [RelayCommand]
    private async Task AskAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(QuestionText))
        {
            return;
        }

        string question = QuestionText.Trim();
        QuestionText = string.Empty;
        IsBusy = true;
        StatusText = "در حال پاسخ‌گویی...";

        try
        {
            await _tutorService.AskAsync(question);
            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseState();
        }
    }

    [RelayCommand]
    private async Task SummarizeNotesAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusText = "در حال خلاصه‌سازی یادداشت‌ها...";

        try
        {
            var notesService = GetNotesService();
            var notes = notesService.GetAll();
            await _tutorService.SummarizeNotesAsync(notes);
            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseState();
        }
    }

    [RelayCommand]
    private async Task GenerateQuizAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusText = "در حال ساخت سوالات...";

        try
        {
            string topic = Session.HasContext
                ? Session.ContextTitle
                : "عمومی";

            await _tutorService.GenerateQuizAsync(topic, 5);
            StatusText = string.Empty;
        }
        catch (Hayt.Licensing.Models.PremiumAccessDeniedException premiumException)
        {
            HandlePremiumAccessDenied(premiumException);
        }

catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseState();
        }
    }

    [RelayCommand]
    private void ClearSession()
    {
        _tutorService.ClearSession();
        RaiseState();
    }

    private IStudyNotesService GetNotesService()
    {
        return new StudyNotesService();
    }

    partial void OnQuestionTextChanged(string value)
    {
        OnPropertyChanged(nameof(CanAsk));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAsk));
    }

    partial void OnSessionChanged(AITutorSession value)
    {
        RaiseState();
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(Messages));
        OnPropertyChanged(nameof(SessionTitle));
        OnPropertyChanged(nameof(SessionContextText));
        OnPropertyChanged(nameof(HasMessages));
        OnPropertyChanged(nameof(CanAsk));
    }

    private async Task<string> RunRealAIAskAsync(string question, string? context = null)
    {
        IAITutorBridgeService bridge = new AITutorBridgeService();
        AITutorBridgeResponse response = await bridge.AskAsync(question, context);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            return response.Content;
        }

        return string.IsNullOrWhiteSpace(response.ErrorMessage)
            ? "پاسخی از مربی هوشمند دریافت نشد."
            : "خطا در مربی هوشمند: " + response.ErrorMessage;
    }

    private async Task<string> RunRealAIToolAsync(string toolName, string input, string? context = null)
    {
        IAITutorBridgeService bridge = new AITutorBridgeService();
        AITutorBridgeResponse response = await bridge.ExecuteToolAsync(toolName, input, context);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            return response.Content;
        }

        return string.IsNullOrWhiteSpace(response.ErrorMessage)
            ? "پاسخی از ابزار هوشمند دریافت نشد."
            : "خطا در ابزار هوشمند: " + response.ErrorMessage;
    }


    private bool _isUpgradeBannerVisible;
    private string _upgradeMessage = "برای استفاده از مربی هوشمند واقعی، لایسنس Premium را فعال کنید.";

    public bool IsUpgradeBannerVisible
    {
        get => _isUpgradeBannerVisible;
        private set { if (_isUpgradeBannerVisible == value) return; _isUpgradeBannerVisible = value; OnPropertyChanged(nameof(IsUpgradeBannerVisible)); }
    }

    public string UpgradeMessage
    {
        get => _upgradeMessage;
        private set { if (string.Equals(_upgradeMessage, value, StringComparison.Ordinal)) return; _upgradeMessage = value; OnPropertyChanged(nameof(UpgradeMessage)); }
    }

    private RelayCommand? _activateLicenseCommand;
    public RelayCommand ActivateLicenseCommand => _activateLicenseCommand ??= new RelayCommand(ExecuteActivateLicense);

    private void ExecuteActivateLicense()
    {
        var service = new Hayt.Services.PremiumUpgradeUIService();
        service.OpenLicenseActivation();
    }

    private void ShowPremiumUpgrade(string? message = null)
    {
        UpgradeMessage = string.IsNullOrWhiteSpace(message) ? "برای استفاده از مربی هوشمند واقعی، لایسنس Premium را فعال کنید." : message.Trim();
        IsUpgradeBannerVisible = true;
    }

    private void HidePremiumUpgrade()
    {
        IsUpgradeBannerVisible = false;
    }

    private void HandlePremiumAccessDenied(Hayt.Licensing.Models.PremiumAccessDeniedException exception)
    {
        ShowPremiumUpgrade(string.IsNullOrWhiteSpace(exception.Message) ? "دسترسی به مدل واقعی هوش مصنوعی نیازمند لایسنس Premium معتبر است." : exception.Message);
    }

    private bool HandlePremiumDeniedResult(Hayt.Models.AIRequestResult? result)
    {
        if (result is null || result.StatusCode != 403) return false;
        ShowPremiumUpgrade(string.IsNullOrWhiteSpace(result.ErrorMessage) ? "برای استفاده از مدل واقعی هوش مصنوعی، لایسنس Premium را فعال کنید." : result.ErrorMessage);
        return true;
    }
}


