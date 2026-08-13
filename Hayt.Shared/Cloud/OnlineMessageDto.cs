using System;

namespace Hayt.Shared.Cloud;

public sealed class OnlineMessageDto
{
    public string FromUserId { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
