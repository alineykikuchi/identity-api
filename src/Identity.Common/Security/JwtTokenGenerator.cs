using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Common.Security;

/// <summary>
/// JWT (JSON Web Token) generator implementation.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the JWT token generator.
    /// </summary>
    /// <param name="configuration">Application configuration holding the keys required for token generation.</param>
    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates a JWT token for a specific user, optionally scoped to one application.
    /// </summary>
    /// <param name="user">User the token will be generated for.</param>
    /// <param name="audience">Client id emitted as <c>aud</c>, or <c>null</c> for no audience.</param>
    /// <param name="roles">Roles emitted as <c>role</c> claims; may be <c>null</c> or empty.</param>
    /// <returns>A valid JWT token as a string.</returns>
    /// <exception cref="ArgumentException">Thrown when the signing key is not configured.</exception>
    public string GenerateToken(IUser user, string? audience = null, IReadOnlyCollection<string>? roles = null)
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username)
        };

        // One claim per role. A user with no roles gets no role claim at all: an empty one
        // would never match [Authorize(Roles = "...")] and only adds noise to the token.
        foreach (var role in EffectiveRoles(user, roles))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            // Null leaves the aud claim out entirely, which is what a login without a
            // client id must produce.
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(JwtSettings.GetAccessTokenMinutes(_configuration)),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Merges the application roles with <see cref="IUser.Role"/>, dropping blanks and
    /// duplicates. The <see cref="IUser"/> role is kept for implementations that carry a
    /// global role of their own; the domain <c>User</c> leaves it empty.
    /// </summary>
    private static IEnumerable<string> EffectiveRoles(IUser user, IReadOnlyCollection<string>? roles)
    {
        var applicationRoles = roles ?? Array.Empty<string>();

        return applicationRoles
            .Append(user.Role)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal);
    }
}
