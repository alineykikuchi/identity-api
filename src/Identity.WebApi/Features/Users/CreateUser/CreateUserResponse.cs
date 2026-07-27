namespace Identity.WebApi.Features.Users.CreateUser;

/// <summary>
/// Response body for a successful registration. Never carries the password hash.
/// </summary>
public class CreateUserResponse
{
    /// <summary>Identifier of the newly created user.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized email of the newly created user.</summary>
    public string Email { get; set; } = string.Empty;
}
