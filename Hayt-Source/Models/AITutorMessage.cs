using System;

namespace Hayt.Models;

public enum AITutorRole
{
    User = 0,
    Assistant = 1,
    System = 2
}

public sealed class AITutorMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public AITutorRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.Now;

    public bool IsUser => Role == AITutorRole.User;

    public bool IsAssistant => Role == AITutorRole.Assistant;

    public string RoleText =>
        Role switch
        {
            AITutorRole.User => "شما",
            AITutorRole.Assistant => "مربی",
            _ => "سیستم"
        };

    public string SentAtText =>
        SentAt.ToString("HH:mm");
}