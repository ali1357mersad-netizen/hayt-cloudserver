using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;

namespace Hayt.ViewModels;

public partial class NotificationViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;

    public ObservableCollection<Notification> Notifications => _notificationService.Notifications;

    public int UnreadCount => Notifications.Count(x => !x.IsRead);

    public bool HasNotifications => Notifications.Count > 0;

    public NotificationViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
        _notificationService.NotificationsChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Notifications));
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasNotifications));
        };
    }

    [RelayCommand]
    public void AddTestInfo()
    {
        _notificationService.AddNotification(
            "اطلاع‌رسانی",
            "سیستم اعلان با موفقیت فعال شد.",
            NotificationType.Info);
        RaiseState();
    }

    [RelayCommand]
    public void AddTestAchievement()
    {
        _notificationService.AddNotification(
            "دستاورد آزمایشی",
            "تبریک! اولین اعلان دستاورد آماده نمایش است.",
            NotificationType.Achievement);
        RaiseState();
    }

    [RelayCommand]
    public void MarkAllRead()
    {
        _notificationService.MarkAllAsRead();
        RaiseState();
    }

    [RelayCommand]
    public void Remove(Notification? notification)
    {
        if (notification is null) return;
        _notificationService.Remove(notification);
        RaiseState();
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(Notifications));
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasNotifications));
    }
}

