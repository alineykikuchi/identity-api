using Microsoft.Extensions.Configuration;

namespace Identity.Common.Security;

/// <summary>
/// Central access to JWT lifetime settings. Keeps the access-token and refresh-token
/// lifetimes configurable (and their defaults in one place) so the token generator and
/// the login/refresh use cases stay in agreement.
/// </summary>
public static class JwtSettings
{
    /// <summary>Configuration key for the access-token lifetime, in minutes.</summary>
    public const string AccessTokenMinutesKey = "Jwt:AccessTokenMinutes";

    /// <summary>Default access-token lifetime, in minutes, when not configured.</summary>
    public const int DefaultAccessTokenMinutes = 15;

    /// <summary>Configuration key for the refresh-token lifetime, in days.</summary>
    public const string RefreshTokenDaysKey = "Jwt:RefreshTokenDays";

    /// <summary>Default refresh-token lifetime, in days, when not configured.</summary>
    public const int DefaultRefreshTokenDays = 7;

    /// <summary>Resolves the access-token lifetime in minutes (falls back to the default).</summary>
    public static int GetAccessTokenMinutes(IConfiguration configuration) =>
        int.TryParse(configuration[AccessTokenMinutesKey], out var minutes) && minutes > 0
            ? minutes
            : DefaultAccessTokenMinutes;

    /// <summary>Resolves the refresh-token lifetime in days (falls back to the default).</summary>
    public static int GetRefreshTokenDays(IConfiguration configuration) =>
        int.TryParse(configuration[RefreshTokenDaysKey], out var days) && days > 0
            ? days
            : DefaultRefreshTokenDays;
}
