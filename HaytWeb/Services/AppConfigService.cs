using HaytWeb.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace HaytWeb.Services;

public class AppConfigService
{
    private readonly IOptions<HaytClientOptions> _options;
    private readonly HttpClient _http;

    public AppConfigService(
        IOptions<HaytClientOptions> options,
        HttpClient http)
    {
        _options = options;
        _http = http;
    }

    public async Task<string> GetOnlineHubUrlAsync()
    {
        try
        {
            var config = await _http.GetFromJsonAsync<AppSettingsDto>("appsettings.json");
            return config?.OnlineHubUrl ?? _options.Value.OnlineHubUrl;
        }
        catch
        {
            return _options.Value.OnlineHubUrl;
        }
    }

    public class AppSettingsDto
    {
        public string OnlineHubUrl { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}
