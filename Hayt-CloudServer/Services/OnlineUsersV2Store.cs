using System.Collections.Concurrent;
using Hayt.Shared.Cloud;

namespace Hayt.CloudServer.Services;

public static class OnlineUsersV2Store
{
    private static readonly ConcurrentDictionary<string, OnlineUserDto> Users = new();

    public static void Upsert(string connectionId, string userId, string displayName, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            return;

        var now = DateTimeOffset.UtcNow;

        Users.AddOrUpdate(
            connectionId,
            _ => new OnlineUserDto
            {
                ConnectionId = connectionId,
                UserId = string.IsNullOrWhiteSpace(userId) ? "default" : userId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "کاربر" : displayName,
                DeviceId = string.IsNullOrWhiteSpace(deviceId) ? Environment.MachineName : deviceId,
                ConnectedAtUtc = now,
                LastSeenUtc = now
            },
            (_, existing) =>
            {
                existing.UserId = string.IsNullOrWhiteSpace(userId) ? existing.UserId : userId;
                existing.DisplayName = string.IsNullOrWhiteSpace(displayName) ? existing.DisplayName : displayName;
                existing.DeviceId = string.IsNullOrWhiteSpace(deviceId) ? existing.DeviceId : deviceId;
                existing.LastSeenUtc = now;
                return existing;
            });
    }

    public static void Touch(string connectionId)
    {
        if (Users.TryGetValue(connectionId, out var user))
        {
            user.LastSeenUtc = DateTimeOffset.UtcNow;
        }
    }

    public static void Remove(string connectionId)
    {
        if (!string.IsNullOrWhiteSpace(connectionId))
        {
            Users.TryRemove(connectionId, out _);
        }
    }

    public static OnlineUsersResponseDto GetAll()
    {
        var users = Users.Values
            .OrderByDescending(x => x.LastSeenUtc)
            .ToList();

        return new OnlineUsersResponseDto
        {
            Count = users.Count,
            Users = users
        };
    }

    public static void Clear()
    {
        Users.Clear();
    }
}
