using Hayt.CloudServer.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Hayt.CloudServer.Hubs;

public class OnlineHub : Hub
{
    private readonly OnlineUserTracker _tracker;

    public OnlineHub(OnlineUserTracker tracker)
    {
        _tracker = tracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Context.User?.FindFirstValue("sub")
                     ?? Context.User?.FindFirstValue("userId");

        var userName = Context.User?.Identity?.Name
                       ?? Context.User?.FindFirstValue(ClaimTypes.Name)
                       ?? Context.User?.FindFirstValue(ClaimTypes.Email);

        var snapshot = _tracker.AddConnection(Context.ConnectionId, userId, userName);

        await Clients.All.SendAsync("UpdateOnlineUsersCount", snapshot.OnlineUsers);
        await Clients.All.SendAsync("UpdateOnlineUsersSnapshot", snapshot);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var snapshot = _tracker.RemoveConnection(Context.ConnectionId);

        await Clients.All.SendAsync("UpdateOnlineUsersCount", snapshot.OnlineUsers);
        await Clients.All.SendAsync("UpdateOnlineUsersSnapshot", snapshot);

        await base.OnDisconnectedAsync(exception);
    }

    public Task<OnlineUsersSnapshot> GetOnlineUsersSnapshot()
    {
        return Task.FromResult(_tracker.CreateSnapshot());
    }
}
