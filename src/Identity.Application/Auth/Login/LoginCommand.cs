using MediatR;

namespace Identity.Application.Auth.Login;

/// <summary>
/// Command carrying the credentials for a login attempt.
/// </summary>
public class LoginCommand : IRequest<LoginResult>
{
    /// <summary>Login email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Plaintext password.</summary>
    public string Password { get; set; } = string.Empty;
}
