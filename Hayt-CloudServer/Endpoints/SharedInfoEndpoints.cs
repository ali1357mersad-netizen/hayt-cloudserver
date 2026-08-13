using Hayt.Shared.Cloud;
using Hayt.Shared.Common;
using Hayt.Shared.Licensing;
using Hayt.Shared.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hayt.CloudServer.Endpoints;

public static class SharedInfoEndpoints
{
    public static IEndpointRouteBuilder MapSharedInfoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/shared")
            .WithTags("Shared");
        group.MapGet("/info", () =>
        {
            var data = new
            {
                shared = "Hayt.Shared",
                status = "Connected",
                version = "1.0.0",
                server = "Hayt.CloudServer",
                endpoints = new[]
                {
                    "/api/v2/shared/info",
                    "/api/v2/shared/sample-user",
                    "/api/v2/shared/sample-online-user"
                }
            };

            return Results.Ok(ApiResponse<object>.Ok(
                data,
                "CloudServer is successfully using Hayt.Shared."
            ));
        });

        group.MapGet("/sample-user", () =>
        {
            var user = new UserDto
            {
                UserId = "local-admin",
                DisplayName = "کاربر اصلی",
                Email = "local-admin@hayt.local",
                IsActive = true,
                Plan = LicensePlan.Premium,
                SubscriptionExpiresAtUtc = DateTimeOffset.UtcNow.AddMonths(1)
            };

            return Results.Ok(ApiResponse<UserDto>.Ok(
                user,
                "Sample shared user loaded successfully."
            ));
        });

        group.MapGet("/sample-online-user", () =>
        {
            var onlineUser = new OnlineUserDto
            {
                UserId = "local-admin",
                DisplayName = "کاربر اصلی",
                DeviceId = Environment.MachineName,
                ConnectionId = "sample-connection",
                ConnectedAtUtc = DateTimeOffset.UtcNow,
                LastSeenUtc = DateTimeOffset.UtcNow
            };

            return Results.Ok(ApiResponse<OnlineUserDto>.Ok(
                onlineUser,
                "Sample shared online user loaded successfully."
            ));
        });

        return app;
    }
}



