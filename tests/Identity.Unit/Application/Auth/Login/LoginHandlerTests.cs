using FluentAssertions;
using Identity.Application.Auth.Login;
using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Unit.Application.Auth.Login;

public class LoginHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
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
            _passwordHasher,
            _jwtTokenGenerator,
            _refreshTokenGenerator,
            _configuration);
    }

    private static LoginCommand ValidCommand => new() { Email = "Person@Example.com", Password = "password123" };

    [Fact(DisplayName = "Valid credentials issue an access token and persist the refresh token as a hash")]
    public async Task Given_ValidCredentials_When_Handling_Then_IssuesTokens()
    {
        var user = new User("person@example.com", "stored-hash");
        _userRepository.GetByEmailAsync("person@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("password123", "stored-hash").Returns(true);
        _jwtTokenGenerator.GenerateToken(Arg.Any<IUser>()).Returns("access-token");
        _refreshTokenGenerator.Generate().Returns("raw-refresh-token");
        _refreshTokenGenerator.Hash("raw-refresh-token").Returns("hashed-refresh-token");
        _refreshTokenRepository.CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<RefreshTokenEntity>());

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
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<IUser>());
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
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<IUser>());
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
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<IUser>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }
}
