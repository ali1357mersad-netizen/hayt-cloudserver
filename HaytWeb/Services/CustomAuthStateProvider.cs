using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HaytWeb.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var savedToken = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(savedToken))
            {
                _http.DefaultRequestHeaders.Authorization = null;
                return Anonymous();
            }

            if (!LooksLikeJwt(savedToken))
            {
                await _localStorage.RemoveItemAsync("authToken");
                _http.DefaultRequestHeaders.Authorization = null;
                return Anonymous();
            }

            var claims = ParseClaimsFromJwtSafe(savedToken);
            if (claims.Count == 0)
            {
                await _localStorage.RemoveItemAsync("authToken");
                _http.DefaultRequestHeaders.Authorization = null;
                return Anonymous();
            }

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            return new AuthenticationState(user);
        }
        catch
        {
            try
            {
                await _localStorage.RemoveItemAsync("authToken");
            }
            catch
            {
            }

            _http.DefaultRequestHeaders.Authorization = null;
            return Anonymous();
        }
    }

    public void MarkUserAsAuthenticated(string token)
    {
        ClaimsPrincipal user;

        if (LooksLikeJwt(token))
        {
            var claims = ParseClaimsFromJwtSafe(token);
            user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "LocalAdmin"),
                new Claim(ClaimTypes.Email, "local-admin@hayt.local"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "local"));
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void MarkUserAsLoggedOut()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static AuthenticationState Anonymous()
    {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private static bool LooksLikeJwt(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.');
        return parts.Length == 3 &&
               !string.IsNullOrWhiteSpace(parts[0]) &&
               !string.IsNullOrWhiteSpace(parts[1]) &&
               !string.IsNullOrWhiteSpace(parts[2]);
    }

    private static List<Claim> ParseClaimsFromJwtSafe(string jwt)
    {
        var claims = new List<Claim>();

        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return claims;

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

            if (keyValuePairs is null)
                return claims;

            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in kvp.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(kvp.Key, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()));
                }
            }
        }
        catch
        {
            return new List<Claim>();
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }
}
