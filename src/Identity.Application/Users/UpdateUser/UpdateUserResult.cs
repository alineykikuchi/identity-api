namespace Identity.Application.Users.UpdateUser;

/// <summary>
/// Profile of the user after the update. Never carries the password hash.
/// </summary>
public class UpdateUserResult
{
    /// <summary>Identifier of the user.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized email of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name after the update.</summary>
    public string? Name { get; set; }
}
