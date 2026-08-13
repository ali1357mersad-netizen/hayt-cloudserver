builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWeb", policy =>
    {
        policy.WithOrigins(
            "https://hayat110.ir", 
            "https://www.hayat110.ir",
            "http://localhost:5137",
            "https://localhost:7260"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
using Hayt.CloudServer.Endpoints;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hayt.CloudServer.Hubs;
using Hayt.CloudServer.Models;
using Hayt.CloudServer.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

var startupTimeUtc = DateTimeOffset.UtcNow;
var stopwatch = Stopwatch.StartNew();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<OnlineUserRegistry>();
builder.Services.AddSingleton<LocalSyncStore>();
builder.Services.AddSingleton<DevelopmentTokenService>();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 1024 * 1024;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("HaytLocalPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 64 * 1024;
})
.AddMessagePackProtocol();

builder.Services.AddSingleton<OnlineUserTracker>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Hayt-Cloud-Server", "Hayt.CloudServer/1.1");
    await next();
});

app.UseCors("HaytLocalPolicy");

app.MapGet("/", () => Results.Ok(new
{
    success = true,
    service = "Hayt.CloudServer",
    message = "Hayt local cloud server is running.",
    version = "1.1.0",
    startupTimeUtc,
    serverTimeUtc = DateTimeOffset.UtcNow,
    endpoints = new
    {
        health = "/api/health",
        onlineUsers = "/api/online/users",
        login = "/api/auth/login",
        syncPush = "/api/cloudsync/push",
        syncPull = "/api/cloudsync/pull",
        signalR = "/hubs/online"
    }
}));

app.MapGet("/api/health", (OnlineUserRegistry registry, IHostEnvironment environment) =>
{
    return Results.Ok(new HealthResponse
    {
        Status = "Healthy",
        Service = "Hayt.CloudServer",
        Version = "1.1.0",
        ServerTimeUtc = DateTimeOffset.UtcNow,
        UptimeSeconds = (long)stopwatch.Elapsed.TotalSeconds,
        OnlineUsers = registry.Count,
        Environment = environment.EnvironmentName
    });
});

app.MapGet("/api/online/users", (OnlineUserRegistry registry) =>
{
    var users = registry.GetAll()
        .Select(x => new
        {
            userId = x.UserId,
            displayName = x.DisplayName,
            deviceId = "",
            connectionId = x.ConnectionId,
            connectedAtUtc = x.ConnectedAtUtc,
            lastSeenUtc = x.LastSeenUtc
        })
        .ToList();

    return Results.Ok(new
    {
        success = true,
        status = "success",
        message = "Online users loaded successfully.",
        data = new
        {
            count = users.Count,
            users
        },
        serverTimeUtc = DateTimeOffset.UtcNow
    });
});

// [REMOVED] app.MapPost("/api/auth/login", (LoginRequest request, DevelopmentTokenService tokenService) =>
// [REMOVED] {
// [REMOVED]     if (string.IsNullOrWhiteSpace(request.UserId))
// [REMOVED]     {
// [REMOVED]         return Results.BadRequest(new LoginResponse
// [REMOVED]         {
// [REMOVED]             Success = false,
// [REMOVED]             Message = "UserId is required."
// [REMOVED]         });
// [REMOVED]     }
// [REMOVED] 
// [REMOVED]     var userId = request.UserId.Trim();
// [REMOVED]     var accessToken = tokenService.CreateToken(userId);
// [REMOVED]     var refreshToken = tokenService.CreateRefreshToken();
// [REMOVED] 
// [REMOVED]     return Results.Ok(new LoginResponse
// [REMOVED]     {
// [REMOVED]         Success = true,
// [REMOVED]         Message = "Local development login completed.",
// [REMOVED]         AccessToken = accessToken,
// [REMOVED]         Token = accessToken,
// [REMOVED]         RefreshToken = refreshToken,
// [REMOVED]         ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(8),
// [REMOVED]         User = new UserInfo
// [REMOVED]         {
// [REMOVED]             Id = $"local-{userId.ToLowerInvariant()}",
// [REMOVED]             Username = userId,
// [REMOVED]             DisplayName = userId
// [REMOVED]         }
// [REMOVED]     });
// [REMOVED] });

app.MapPost("/api/auth/refresh", (RefreshTokenRequest request, DevelopmentTokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Refresh token is required."
        });
    }

    var accessToken = tokenService.CreateToken("local-user");

    return Results.Ok(new
    {
        success = true,
        accessToken,
        token = accessToken,
        refreshToken = tokenService.CreateRefreshToken(),
        expiresAtUtc = DateTimeOffset.UtcNow.AddHours(8)
    });
});

app.MapPost("/api/auth/logout", () => Results.Ok(new
{
    success = true,
    message = "Logged out successfully."
}));

StoredSyncItem SaveSyncLikeRequest(object? request, LocalSyncStore store)
{
    if (request is null)
    {
        return store.Add(new SyncPushRequest
        {
            UserId = "local-user",
            DeviceId = "local-device",
            EntityType = "unknown",
            EntityId = Guid.NewGuid().ToString("N"),
            Data = null
        });
    }

    return store.Add(new SyncPushRequest
    {
        UserId = "local-user",
        DeviceId = "local-device",
        EntityType = "batch",
        EntityId = Guid.NewGuid().ToString("N"),
        Data = request
    });
}

app.MapPost("/api/sync/push", (object request, LocalSyncStore store) =>
{
    var savedItem = SaveSyncLikeRequest(request, store);

    return Results.Ok(new
    {
        success = true,
        message = "Sync item stored locally.",
        acceptedIds = new[] { savedItem.Id },
        rejectedItems = Array.Empty<object>(),
        serverTimeUtc = DateTimeOffset.UtcNow
    });
});

app.MapPost("/api/cloudsync/push", (object request, LocalSyncStore store) =>
{
    var savedItem = SaveSyncLikeRequest(request, store);

    return Results.Ok(new
    {
        success = true,
        message = "Sync item stored locally.",
        acceptedIds = new[] { savedItem.Id },
        rejectedItems = Array.Empty<object>(),
        serverTimeUtc = DateTimeOffset.UtcNow
    });
});

app.MapGet("/api/sync/pull", (string? userId, DateTimeOffset? sinceUtc, LocalSyncStore store) =>
{
    var items = store.Get(userId, sinceUtc)
        .Select(x => new
        {
            id = x.Id,
            entityType = x.EntityType,
            entityId = x.EntityId,
            operationType = "upsert",
            payloadJson = JsonSerializer.Serialize(x.Data),
            createdAtUtc = x.ReceivedAtUtc
        })
        .ToList();

    return Results.Ok(new
    {
        success = true,
        message = "Sync items loaded.",
        items,
        serverTimeUtc = DateTimeOffset.UtcNow
    });
});

app.MapPost("/api/cloudsync/pull", (object request, LocalSyncStore store) =>
{
    var items = store.Get(null, null)
        .Select(x => new
        {
            id = x.Id,
            entityType = x.EntityType,
            entityId = x.EntityId,
            operationType = "upsert",
            payloadJson = JsonSerializer.Serialize(x.Data),
            createdAtUtc = x.ReceivedAtUtc
        })
        .ToList();

    return Results.Ok(new
    {
        success = true,
        message = "Sync items loaded.",
        items,
        serverTimeUtc = DateTimeOffset.UtcNow
    });
});

app.MapPost("/api/messages/broadcast", async (CloudMessageRequest request, IHubContext<OnlineHub> hubContext) =>
{
    await hubContext.Clients.All.SendAsync("PublicMessageReceived", new
    {
        fromUserId = request.UserId ?? "system",
        fromDisplayName = request.UserId ?? "system",
        message = request.Message ?? "",
        sentAtUtc = DateTimeOffset.UtcNow
    });

    return Results.Ok(new
    {
        success = true,
        message = "Message broadcast completed.",
        serverTimeUtc = DateTimeOffset.UtcNow
    });
});

app.MapHub<OnlineHub>("/hubs/online");
app.MapHub<OnlineHub>("/onlineHub");
app.MapHub<OnlineHub>("/hub/online");
app.MapHub<OnlineHub>("/cloudHub");

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("Hayt Cloud Server started at {Time}", DateTimeOffset.Now);
    app.Logger.LogInformation("HTTP address: http://localhost:5088");
    app.Logger.LogInformation("SignalR Hub: http://localhost:5088/hubs/online");
});

app.MapSharedInfoEndpoints();

app.MapOnlineUsersV2Endpoints();
app.MapLicenseEndpoints();
app.MapAuthEndpoints();
app.Run();









