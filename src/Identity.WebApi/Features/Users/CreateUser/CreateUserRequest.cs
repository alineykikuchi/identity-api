namespace Identity.WebApi.Features.Users.CreateUser;

/// <summary>
/// Request body for <c>POST /api/register</c>.
/// </summary>
public class CreateUserRequest
{
    /// <summary>Login email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Plaintext password (minimum 8 characters).</summary>
    public string Password { get; set; } = string.Empty;
}
