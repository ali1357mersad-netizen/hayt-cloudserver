using System;
using System.Collections.ObjectModel;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public interface INotificationService
{
    ObservableCollection<Notification> Notifications { get; }
    event EventHandler? NotificationsChanged;
    void AddNotification(string title, string message, NotificationType type);
    void MarkAllAsRead();
    void Remove(Notification notification);
}

