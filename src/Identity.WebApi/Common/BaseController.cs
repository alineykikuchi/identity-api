using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Identity.WebApi.Common;

/// <summary>
/// Base class for controllers that need to know who the authenticated caller is.
/// Keeps claim reading in one place instead of repeating it in every endpoint.
/// </summary>
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Reads the authenticated user's identifier from the <c>sub</c> claim of the access token.
    /// </summary>
    /// <returns>The identifier of the authenticated user.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the claim is missing or is not a valid identifier. On an
    /// <c>[Authorize]</c> endpoint this only happens with a token this API did not issue.
    /// </exception>
    protected Guid GetCurrentUserId()
    {
        // Both names are read on purpose. With the JwtBearer default MapInboundClaims the
        // token's "sub" arrives renamed to ClaimTypes.NameIdentifier; with the mapping
        // turned off it arrives as "sub". Reading only one of them makes this method
        // silently depend on that setting.
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("Unauthorized.");
        }

        return userId;
    }
}
