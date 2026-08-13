using System.Collections.Concurrent;

namespace Hayt.CloudServer.Services;

public sealed class OnlineUserTracker
{
    private readonly ConcurrentDictionary<string, OnlineUserSession> _connections = new();

    public int OnlineConnectionsCount => _connections.Count;

    public int OnlineUsersCount
    {
        get
        {
            return _connections.Values
                .Select(x => x.UserKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }
    }

    public IReadOnlyCollection<OnlineUserSession> GetSessions()
    {
        return _connections.Values.ToList().AsReadOnly();
    }

    public OnlineUsersSnapshot AddConnection(string connectionId, string? userId, string? userName)
    {
        var safeUserKey = NormalizeUserKey(connectionId, userId, userName);

        var session = new OnlineUserSession
        {
            ConnectionId = connectionId,
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            UserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim(),
            UserKey = safeUserKey,
            ConnectedAtUtc = DateTime.UtcNow
        };

        _connections[connectionId] = session;

        return CreateSnapshot();
    }

    public OnlineUsersSnapshot RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        return CreateSnapshot();
    }

    public OnlineUsersSnapshot CreateSnapshot()
    {
        return new OnlineUsersSnapshot
        {
            OnlineConnections = OnlineConnectionsCount,
            OnlineUsers = OnlineUsersCount,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    private static string NormalizeUserKey(string connectionId, string? userId, string? userName)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            return userId.Trim();

        if (!string.IsNullOrWhiteSpace(userName))
            return userName.Trim();

        return "anonymous:" + connectionId;
    }
}

public sealed class OnlineUserSession
{
    public string ConnectionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string UserKey { get; set; } = string.Empty;
    public DateTime ConnectedAtUtc { get; set; }
}

public sealed class OnlineUsersSnapshot
{
    public int OnlineUsers { get; set; }
    public int OnlineConnections { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
