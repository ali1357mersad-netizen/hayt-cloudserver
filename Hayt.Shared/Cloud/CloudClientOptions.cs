using System;

namespace Hayt.Shared.Cloud;

public sealed class CloudClientOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5088";
    public string ApiKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = Environment.MachineName;
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxRetryCount { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 700;
}
