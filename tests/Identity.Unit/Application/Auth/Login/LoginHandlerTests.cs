using FluentAssertions;
using Identity.Application.Auth.Login;
using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using ApplicationEntity = Identity.Domain.Entities.Application;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Unit.Application.Auth.Login;

public class LoginHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IApplicationRepository _applicationRepository = Substitute.For<IApplicationRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IRefreshTokenGenerator _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _handler = new LoginHandler(
            _userRepository,
            _refreshTokenRepository,
            _applicationRepository,
            _roleRepository,
            _passwordHasher,
            _jwtTokenGenerator,
            _refreshTokenGenerator,
            _configuration);
    }

    private static LoginCommand ValidCommand => new() { Email = "Person@Example.com", Password = "password123" };

    private static LoginCommand CommandFor(string clientId) => new()
    {
        Email = "Person@Example.com",
        Password = "password123",
        ClientId = clientId
    };

    /// <summary>Wires up a user whose password verifies, plus the token generators.</summary>
    private User GivenAuthenticatedUser()
    {
        var user = new User("person@example.com", "stored-hash");
        _userRepository.GetByEmailAsync("person@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("password123", "stored-hash").Returns(true);
        _jwtTokenGenerator
            .GenerateToken(Arg.Any<IUser>(), Arg.Any<string?>(), Arg.Any<IReadOnlyCollection<string>?>())
            .Returns("access-token");
        _refreshTokenGenerator.Generate().Returns("raw-refresh-token");
        _refreshTokenGenerator.Hash("raw-refresh-token").Returns("hashed-refresh-token");
        _refreshTokenRepository.CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<RefreshTokenEntity>());

        return user;
    }

    [Fact(DisplayName = "Valid credentials issue an access token and persist the refresh token as a hash")]
    public async Task Given_ValidCredentials_When_Handling_Then_IssuesTokens()
    {
        var user = GivenAuthenticatedUser();

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        result.ExpiresIn.Should().Be(900); // default 15 minutes

        await _refreshTokenRepository.Received(1).CreateAsync(
            Arg.Is<RefreshTokenEntity>(rt => rt.UserId == user.Id
                && rt.TokenHash == "hashed-refresh-token"
                && rt.TokenHash != "raw-refresh-token"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An unknown email returns the generic unauthorized error and issues nothing")]
    public async Task Given_UnknownEmail_When_Handling_Then_ThrowsUnauthorized()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(ValidCommand, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _jwtTokenGenerator.DidNotReceive().GenerateToken(
            Arg.Any<IUser>(), Arg.Any<string?>(), Arg.Any<IReadOnlyCollection<string>?>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A wrong password returns the generic unauthorized error and issues nothing")]
    public async Task Given_WrongPassword_When_Handling_Then_ThrowsUnauthorized()
    {
        var user = new User("person@example.com", "stored-hash");
        _userRepository.GetByEmailAsync("person@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = () => _handler.Handle(ValidCommand, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _jwtTokenGenerator.DidNotReceive().GenerateToken(
            Arg.Any<IUser>(), Arg.Any<string?>(), Arg.Any<IReadOnlyCollection<string>?>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An inactive account returns the generic unauthorized error even with the right password")]
    public async Task Given_InactiveUser_When_Handling_Then_ThrowsUnauthorized()
    {
        var user = new User("person@example.com", "stored-hash");
        user.Deactivate();
        _userRepository.GetByEmailAsync("person@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("password123", "stored-hash").Returns(true);

        var act = () => _handler.Handle(ValidCommand, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _jwtTokenGenerator.DidNotReceive().GenerateToken(
            Arg.Any<IUser>(), Arg.Any<string?>(), Arg.Any<IReadOnlyCollection<string>?>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Without a client id the token carries no audience and no roles")]
    public async Task Given_NoClientId_When_Handling_Then_IssuesTokenWithoutAudienceOrRoles()
    {
        GivenAuthenticatedUser();

        await _handler.Handle(ValidCommand, CancellationToken.None);

        _jwtTokenGenerator.Received(1).GenerateToken(
            Arg.Any<IUser>(),
            null,
            Arg.Is<IReadOnlyCollection<string>?>(roles => roles!.Count == 0));

        // No application was consulted at all.
        await _applicationRepository.DidNotReceive().GetByClientIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _roleRepository.DidNotReceive().GetByUserAndApplicationAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        // The refresh token is not tied to any application either.
        await _refreshTokenRepository.Received(1).CreateAsync(
            Arg.Is<RefreshTokenEntity>(rt => rt.ApplicationId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A valid client id puts that application's audience and the user's roles in the token")]
    public async Task Given_ValidClientId_When_Handling_Then_IssuesTokenWithAudienceAndRoles()
    {
        var user = GivenAuthenticatedUser();
        var application = new ApplicationEntity("sample-app", "Sample App");

        _applicationRepository.GetByClientIdAsync("sample-app", Arg.Any<CancellationToken>()).Returns(application);
        _roleRepository.GetByUserAndApplicationAsync(user.Id, application.Id, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new Role(application.Id, "Admin"),
                new Role(application.Id, "Customer")
            });

        await _handler.Handle(CommandFor("sample-app"), CancellationToken.None);

        _jwtTokenGenerator.Received(1).GenerateToken(
            Arg.Any<IUser>(),
            "sample-app",
            Arg.Is<IReadOnlyCollection<string>?>(roles =>
                roles!.Count == 2 && roles.Contains("Admin") && roles.Contains("Customer")));

        // The refresh token records the application so rotation keeps the same scope.
        await _refreshTokenRepository.Received(1).CreateAsync(
            Arg.Is<RefreshTokenEntity>(rt => rt.ApplicationId == application.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An unknown client id returns the generic unauthorized error")]
    public async Task Given_UnknownClientId_When_Handling_Then_ThrowsUnauthorized()
    {
        GivenAuthenticatedUser();
        _applicationRepository.GetByClientIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ApplicationEntity?)null);

        var act = () => _handler.Handle(CommandFor("ghost-app"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _jwtTokenGenerator.DidNotReceive().GenerateToken(
            Arg.Any<IUser>(), Arg.Any<string?>(), Arg.Any<IReadOnlyCollection<string>?>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An inactive application returns the generic unauthorized error")]
    public async Task Given_InactiveApplication_When_Handling_Then_ThrowsUnauthorized()
    {
        GivenAuthenticatedUser();
        var application = new ApplicationEntity("retired-app", "Retired App");
        application.Deactivate();
        _applicationRepository.GetByClientIdAsync("retired-app", Arg.Any<CancellationToken>()).Returns(application);

        var act = () => _handler.Handle(CommandFor("retired-app"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _jwtTokenGenerator.DidNotReceive().GenerateToken(
            Arg.Any<IUser>(), Arg.Any<string?>(), Arg.Any<IReadOnlyCollection<string>?>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A user with no roles in the application still logs in, with no role claims")]
    public async Task Given_UserWithoutRoles_When_Handling_Then_IssuesTokenWithoutRoles()
    {
        var user = GivenAuthenticatedUser();
        var application = new ApplicationEntity("sample-app", "Sample App");

        _applicationRepository.GetByClientIdAsync("sample-app", Arg.Any<CancellationToken>()).Returns(application);
        _roleRepository.GetByUserAndApplicationAsync(user.Id, application.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Role>());

        var result = await _handler.Handle(CommandFor("sample-app"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        _jwtTokenGenerator.Received(1).GenerateToken(
            Arg.Any<IUser>(),
            "sample-app",
            Arg.Is<IReadOnlyCollection<string>?>(roles => roles!.Count == 0));
    }

    [Fact(DisplayName = "Roles are looked up for the requested application only, so other applications' roles cannot leak in")]
    public async Task Given_ClientId_When_Handling_Then_QueriesRolesForThatApplicationOnly()
    {
        var user = GivenAuthenticatedUser();
        var requested = new ApplicationEntity("sample-app", "Sample App");
        var other = new ApplicationEntity("other-app", "Other App");

        _applicationRepository.GetByClientIdAsync("sample-app", Arg.Any<CancellationToken>()).Returns(requested);
        _roleRepository.GetByUserAndApplicationAsync(user.Id, requested.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new Role(requested.Id, "Customer") });
        _roleRepository.GetByUserAndApplicationAsync(user.Id, other.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new Role(other.Id, "Admin") });

        await _handler.Handle(CommandFor("sample-app"), CancellationToken.None);

        await _roleRepository.Received(1).GetByUserAndApplicationAsync(
            user.Id, requested.Id, Arg.Any<CancellationToken>());
        await _roleRepository.DidNotReceive().GetByUserAndApplicationAsync(
            user.Id, other.Id, Arg.Any<CancellationToken>());

        _jwtTokenGenerator.Received(1).GenerateToken(
            Arg.Any<IUser>(),
            "sample-app",
            Arg.Is<IReadOnlyCollection<string>?>(roles => roles!.Count == 1 && roles.Contains("Customer")));
    }
}
