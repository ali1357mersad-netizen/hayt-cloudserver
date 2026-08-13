using System;

namespace Hayt.Shared.Cloud;

public sealed class OnlineUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OnlineUsersResponseDto
{
    public int Count { get; set; }
    public List<OnlineUserDto> Users { get; set; } = new();
}
