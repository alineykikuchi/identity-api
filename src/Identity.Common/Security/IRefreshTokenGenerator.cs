namespace Identity.Common.Security;

/// <summary>
/// Creates opaque refresh tokens and hashes them for storage. Only the hash is ever
/// persisted, so a database leak cannot expose usable tokens.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Generates a new cryptographically random refresh token (the plaintext value).</summary>
    string Generate();

    /// <summary>Computes the SHA-256 hash of a refresh token, used as its stored representation.</summary>
    string Hash(string token);
}
