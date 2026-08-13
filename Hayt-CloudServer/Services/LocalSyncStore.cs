using System.Collections.Concurrent;
using Hayt.CloudServer.Models;

namespace Hayt.CloudServer.Services;

public sealed class LocalSyncStore
{
    private readonly ConcurrentDictionary<string, StoredSyncItem> _items = new();

    public int Count => _items.Count;

    public StoredSyncItem Add(SyncPushRequest request)
    {
        var item = new StoredSyncItem
        {
            UserId = string.IsNullOrWhiteSpace(request.UserId) ? "local-user" : request.UserId.Trim(),
            DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? "local-device" : request.DeviceId.Trim(),
            EntityType = string.IsNullOrWhiteSpace(request.EntityType) ? "unknown" : request.EntityType.Trim(),
            EntityId = string.IsNullOrWhiteSpace(request.EntityId) ? Guid.NewGuid().ToString("N") : request.EntityId.Trim(),
            Data = request.Data
        };

        _items[item.Id] = item;
        return item;
    }

    public IReadOnlyCollection<StoredSyncItem> Get(string? userId, DateTimeOffset? sinceUtc)
    {
        IEnumerable<StoredSyncItem> query = _items.Values;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(item =>
                string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase));
        }

        if (sinceUtc.HasValue)
        {
            query = query.Where(item => item.ReceivedAtUtc >= sinceUtc.Value);
        }

        return query.OrderBy(item => item.ReceivedAtUtc).ToArray();
    }
}
