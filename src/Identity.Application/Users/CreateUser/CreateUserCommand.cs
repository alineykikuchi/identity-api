using MediatR;

namespace Identity.Application.Users.CreateUser;

/// <summary>
/// Command to register a new user with an email and a plaintext password.
/// </summary>
public class CreateUserCommand : IRequest<CreateUserResult>
{
    /// <summary>Login email. Normalized (trim + lowercase) by the handler.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Plaintext password. Hashed by the handler; never persisted as-is.</summary>
    public string Password { get; set; } = string.Empty;
}
