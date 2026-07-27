namespace Identity.Application.Users.CreateUser;

/// <summary>
/// Result of registering a user. Never carries the password or its hash.
/// </summary>
public class CreateUserResult
{
    /// <summary>Identifier of the newly created user.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized email of the newly created user.</summary>
    public string Email { get; set; } = string.Empty;
}
