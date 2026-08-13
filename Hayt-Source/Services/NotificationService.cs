using System;
using System.Collections.ObjectModel;
using System.Linq;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services;

public class NotificationService : INotificationService
{
    private int _lastId;

    public ObservableCollection<Notification> Notifications { get; } = new();

    public event EventHandler? NotificationsChanged;

    public void AddNotification(string title, string message, NotificationType type)
    {
        var item = new Notification
        {
            Id = ++_lastId,
            Title = title,
            Message = message,
            Type = type,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        Notifications.Insert(0, item);
        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MarkAllAsRead()
    {
        foreach (var item in Notifications.Where(x => !x.IsRead))
        {
            item.IsRead = true;
        }
        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Notification notification)
    {
        if (notification is null) return;
        if (Notifications.Contains(notification))
        {
            Notifications.Remove(notification);
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

