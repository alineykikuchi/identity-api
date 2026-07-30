using System.Security.Cryptography;
using System.Text;

namespace Identity.Common.Security;

/// <summary>
/// Default <see cref="IRefreshTokenGenerator"/>: 64 bytes of cryptographic randomness
/// encoded as Base64Url, hashed with SHA-256 (hex) for storage.
/// </summary>
public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 64;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        return Base64UrlEncode(bytes);
    }

    public string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
