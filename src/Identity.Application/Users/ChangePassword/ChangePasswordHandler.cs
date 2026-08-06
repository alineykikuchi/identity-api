using Identity.Common.Security;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Users.ChangePassword;

/// <summary>
/// Replaces the password of the authenticated user and drops every session.
/// </summary>
/// <remarks>
/// Revoking all refresh tokens signs the account out of other devices, which is the point
/// of changing a password that may have leaked. The current client is signed out as well
/// and has to log in again. Access tokens already issued stay valid until they expire
/// (at most 15 minutes) — the expected behaviour of stateless JWTs.
/// </remarks>
public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
{
    private const string InvalidUser = "Unauthorized.";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException(InvalidUser);
        }

        if (!_passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash))
        {
            // Generic message: never state which part of the request was wrong.
            throw new UnauthorizedAccessException(InvalidUser);
        }

        user.ChangePassword(_passwordHasher.HashPassword(command.NewPassword));
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _refreshTokenRepository.RevokeAllByUserAsync(user.Id, cancellationToken);
    }
}
