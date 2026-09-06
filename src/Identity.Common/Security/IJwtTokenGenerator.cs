namespace Identity.Common.Security;

/// <summary>
/// Contract for JWT token generation.
/// </summary>
/// <remarks>
/// The current implementation signs tokens with HS256 (symmetric key). Migrating
/// to RS256 + JWKS must not require changing this contract.
/// </remarks>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates an access token for a user, optionally scoped to one application.
    /// </summary>
    /// <param name="user">User the token is issued for.</param>
    /// <param name="audience">
    /// Client id of the application the token is meant for, emitted as the <c>aud</c> claim.
    /// When <c>null</c> the token carries no audience and is not tied to any application.
    /// </param>
    /// <param name="roles">
    /// Roles the user holds in that application, emitted as one <c>role</c> claim each.
    /// May be <c>null</c> or empty, in which case the token carries no role claim.
    /// </param>
    string GenerateToken(IUser user, string? audience = null, IReadOnlyCollection<string>? roles = null);
}
