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
    string GenerateToken(IUser user);
}
