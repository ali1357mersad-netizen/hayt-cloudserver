using System.Collections.Concurrent;
using Hayt.CloudServer.Models;

namespace Hayt.CloudServer.Services;

public sealed class OnlineUserRegistry
{
    private readonly ConcurrentDictionary<string, OnlineUser> _users = new();

    public int Count => _users.Count;

    public IReadOnlyCollection<OnlineUser> GetAll()
    {
        return _users.Values
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.ConnectedAtUtc)
            .ToArray();
    }

    public OnlineUser AddOrUpdate(
        string connectionId,
        string? userId,
        string? displayName,
        string? platform)
    {
        var normalizedUserId = string.IsNullOrWhiteSpace(userId)
            ? $"guest-{connectionId[..Math.Min(8, connectionId.Length)]}"
            : userId.Trim();

        var normalizedDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? normalizedUserId
            : displayName.Trim();

        var now = DateTimeOffset.UtcNow;

        var user = new OnlineUser
        {
            ConnectionId = connectionId,
            UserId = normalizedUserId,
            DisplayName = normalizedDisplayName,
            Platform = string.IsNullOrWhiteSpace(platform) ? "Hayt-WPF" : platform.Trim(),
            ConnectedAtUtc = now,
            LastSeenUtc = now
        };

        _users.AddOrUpdate(connectionId, user, (_, _) => user);
        return user;
    }

    public bool Touch(string connectionId)
    {
        if (!_users.TryGetValue(connectionId, out var user))
            return false;

        user.LastSeenUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public bool Remove(string connectionId, out OnlineUser? user)
    {
        return _users.TryRemove(connectionId, out user);
    }
}
