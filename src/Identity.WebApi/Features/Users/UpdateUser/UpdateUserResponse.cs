namespace Identity.WebApi.Features.Users.UpdateUser;

/// <summary>
/// Response body for <c>PUT /api/me</c>. Never carries the password hash.
/// </summary>
public class UpdateUserResponse
{
    /// <summary>Identifier of the user.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized email of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name after the update.</summary>
    public string? Name { get; set; }
}
