using Hayt.CloudServer.Services;
using Hayt.Shared.Cloud;
using Hayt.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hayt.CloudServer.Endpoints;

public static class OnlineUsersV2Endpoints
{
    public static IEndpointRouteBuilder MapOnlineUsersV2Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/online")
            .WithTags("Online V2");

        group.MapGet("/users", () =>
        {
            OnlineUsersResponseDto data = OnlineUsersV2Store.GetAll();

            return Results.Ok(ApiResponse<OnlineUsersResponseDto>.Ok(
                data,
                "Online users loaded successfully from Hayt.Shared."
            ));
        });

        group.MapPost("/clear", () =>
        {
            OnlineUsersV2Store.Clear();

            var data = OnlineUsersV2Store.GetAll();

            return Results.Ok(ApiResponse<OnlineUsersResponseDto>.Ok(
                data,
                "Online users cleared successfully."
            ));
        });

        return app;
    }
}

