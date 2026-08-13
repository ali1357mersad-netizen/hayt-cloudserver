using System.Security.Cryptography;
using System.Text;

namespace Hayt.CloudServer.Services;

public sealed class DevelopmentTokenService
{
    public string CreateToken(string username)
    {
        var payload = $"{username}|{DateTimeOffset.UtcNow:O}|{Guid.NewGuid():N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }
}
