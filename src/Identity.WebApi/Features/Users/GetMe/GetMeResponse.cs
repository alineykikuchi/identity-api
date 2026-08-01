namespace Identity.WebApi.Features.Users.GetMe;

/// <summary>
/// Response body for <c>GET /api/me</c>. Never carries the password hash.
/// </summary>
public class GetMeResponse
{
    /// <summary>Identifier of the authenticated user.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized email of the authenticated user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional display name of the authenticated user.</summary>
    public string? Name { get; set; }
}
