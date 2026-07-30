namespace Identity.WebApi.Features.Auth.Login;

/// <summary>
/// Request body for <c>POST /api/login</c>.
/// </summary>
public class LoginRequest
{
    /// <summary>Login email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Plaintext password.</summary>
    public string Password { get; set; } = string.Empty;
}
