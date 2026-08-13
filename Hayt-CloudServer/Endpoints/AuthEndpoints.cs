using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hayt.CloudServer.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/login", (LoginRequest req) =>
        {
            if (req.Email == "local-admin@hayt.local" && req.Password == "123456")
                return Results.Ok(new { success = true, token = "hayt_premium_token_2026", message = "ورود موفقیت‌آمیز" });
            return Results.Unauthorized();
        });
        group.MapPost("/register", (RegisterRequest req) => Results.Ok(new { success = true, message = "ثبت‌نام انجام شد" }));
        return app;
    }
    public class LoginRequest { public string? Email { get; set; } public string? Password { get; set; } }
    public class RegisterRequest { public string? Name { get; set; } public string? Email { get; set; } public string? Password { get; set; } }
}
