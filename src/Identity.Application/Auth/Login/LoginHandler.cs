using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.Auth.Login;

/// <summary>
/// Authenticates a user and issues an access token plus a refresh token.
/// </summary>
/// <remarks>
/// Every failure mode (unknown email, wrong password, inactive account) returns the same
/// generic error, and a BCrypt verification runs even for unknown emails, so responses do
/// not leak which check failed — neither by content nor by timing.
/// </remarks>
public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private const string InvalidCredentials = "Invalid credentials.";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IConfiguration _configuration;

    // Computed once, lazily: a valid hash to verify against when the email is unknown,
    // so that path costs the same as a real verification.
    private static string? _dummyPasswordHash;

    public LoginHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _configuration = configuration;
    }

    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = User.Normalize(command.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            // Spend the same work as a real verification to avoid a timing oracle.
            _passwordHasher.VerifyPassword(command.Password, DummyPasswordHash);
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var accessTokenMinutes = JwtSettings.GetAccessTokenMinutes(_configuration);
        var refreshTokenDays = JwtSettings.GetRefreshTokenDays(_configuration);

        var rawRefreshToken = _refreshTokenGenerator.Generate();
        var refreshToken = new RefreshTokenEntity(
            user.Id,
            _refreshTokenGenerator.Hash(rawRefreshToken),
            DateTime.UtcNow.AddDays(refreshTokenDays));

        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);

        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresIn = accessTokenMinutes * 60
        };
    }

    private string DummyPasswordHash =>
        _dummyPasswordHash ??= _passwordHasher.HashPassword("timing-parity-placeholder");
}
