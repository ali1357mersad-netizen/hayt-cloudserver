using System;

namespace Hayt.Models;

public enum NotificationType
{
    Info = 0,
    Achievement = 1,
    Warning = 2
}

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public NotificationType Type { get; set; }
}