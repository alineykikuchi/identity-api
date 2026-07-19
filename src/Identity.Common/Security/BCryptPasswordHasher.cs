namespace Identity.Common.Security;

/// <summary>
/// Implements password hashing using the BCrypt algorithm.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using the BCrypt algorithm.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The BCrypt hashed password.</returns>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies whether a plain text password matches a BCrypt hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The BCrypt hash to compare against.</param>
    /// <returns>True if the password matches the hash, false otherwise.</returns>
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
