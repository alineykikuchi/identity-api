using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.WebApi.Common;

/// <summary>
/// Base class for controllers that need to know who the authenticated caller is.
/// Keeps claim reading in one place instead of repeating it in every endpoint.
/// </summary>
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Reads the authenticated user's identifier from the <see cref="ClaimTypes.NameIdentifier"/>
    /// claim of the access token.
    /// </summary>
    /// <returns>The identifier of the authenticated user.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the claim is missing or is not a valid identifier. On an
    /// <c>[Authorize]</c> endpoint this only happens with a token this API did not issue.
    /// </exception>
    protected Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("Unauthorized.");
        }

        return userId;
    }
}
