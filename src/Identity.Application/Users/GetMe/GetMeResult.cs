namespace Identity.Application.Users.GetMe;

/// <summary>
/// Profile data of the authenticated user. Never carries the password hash.
/// </summary>
public class GetMeResult
{
    /// <summary>Identifier of the user.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized email of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional display name of the user.</summary>
    public string? Name { get; set; }
}
