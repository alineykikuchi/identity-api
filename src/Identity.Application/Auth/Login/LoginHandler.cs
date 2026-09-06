using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using ApplicationEntity = Identity.Domain.Entities.Application;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.Auth.Login;

/// <summary>
/// Authenticates a user and issues an access token plus a refresh token.
/// </summary>
/// <remarks>
/// Every failure mode (unknown email, wrong password, inactive account, unknown or inactive
/// application) returns the same generic error, and a BCrypt verification runs even for
/// unknown emails, so responses do not leak which check failed — neither by content nor by
/// timing.
///
/// When the command carries a client id the token is scoped to that application: it gets the
/// application's audience and the roles the user holds there. Without one the token is issued
/// with neither, which keeps existing clients working unchanged.
/// </remarks>
public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private const string InvalidCredentials = "Invalid credentials.";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IRoleRepository _roleRepository;
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
        IApplicationRepository applicationRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _applicationRepository = applicationRepository;
        _roleRepository = roleRepository;
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

        // Resolved after the credential checks so an anonymous caller cannot probe which
        // client ids are registered.
        var (application, roles) = await ResolveApplicationAsync(command.ClientId, user.Id, cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(user, application?.ClientId, roles);
        var accessTokenMinutes = JwtSettings.GetAccessTokenMinutes(_configuration);
        var refreshTokenDays = JwtSettings.GetRefreshTokenDays(_configuration);

        var rawRefreshToken = _refreshTokenGenerator.Generate();
        var refreshToken = new RefreshTokenEntity(
            user.Id,
            _refreshTokenGenerator.Hash(rawRefreshToken),
            DateTime.UtcNow.AddDays(refreshTokenDays),
            application?.Id);

        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);

        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresIn = accessTokenMinutes * 60
        };
    }

    /// <summary>
    /// Resolves the application the login is for and the roles the user holds in it. Returns
    /// no application and no roles when the command carries no client id.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the client id does not match a registered, active application.
    /// </exception>
    private async Task<(ApplicationEntity? Application, IReadOnlyCollection<string> Roles)> ResolveApplicationAsync(
        string? clientId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return (null, Array.Empty<string>());
        }

        var application = await _applicationRepository.GetByClientIdAsync(clientId, cancellationToken);

        if (application is null || !application.IsActive)
        {
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        var applicationRoles = await _roleRepository.GetByUserAndApplicationAsync(
            userId,
            application.Id,
            cancellationToken);

        // A user with no roles in the application still logs in; the token simply carries
        // no role claim. Fine-grained authorization is each consuming application's call.
        return (application, applicationRoles.Select(role => role.Name).ToArray());
    }

    private string DummyPasswordHash =>
        _dummyPasswordHash ??= _passwordHasher.HashPassword("timing-parity-placeholder");
}
