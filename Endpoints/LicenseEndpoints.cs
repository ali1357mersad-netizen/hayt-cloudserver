using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hayt.CloudServer.Endpoints;

public static class LicenseEndpoints
{
    public static IEndpointRouteBuilder MapLicenseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/license/check", (string email) =>
        {
            if (email == "local-admin@hayt.local") return Results.Ok(new { email, plan = "Premium", licensed = true });
            return Results.Ok(new { email, plan = "Free", licensed = false });
        });
        return app;
    }
}
