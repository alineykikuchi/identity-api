using FluentAssertions;
using Identity.Application.Auth.RefreshToken;
using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Unit.Application.Auth.RefreshToken;

public class RefreshTokenHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IRefreshTokenGenerator _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _handler = new RefreshTokenHandler(
            _refreshTokenRepository,
            _userRepository,
            _jwtTokenGenerator,
            _refreshTokenGenerator,
            _configuration);
    }

    private static RefreshTokenCommand Command(string token = "raw-token") => new() { RefreshToken = token };

    [Fact(DisplayName = "A valid refresh token is rotated: old one revoked, a new pair issued")]
    public async Task Given_ValidToken_When_Handling_Then_RotatesAndIssuesNewPair()
    {
        var user = new User("person@example.com", "stored-hash");
        var stored = new RefreshTokenEntity(user.Id, "stored-token-hash", DateTime.UtcNow.AddDays(7));

        _refreshTokenGenerator.Hash("raw-token").Returns("stored-token-hash");
        _refreshTokenRepository.GetByTokenHashAsync("stored-token-hash", Arg.Any<CancellationToken>()).Returns(stored);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _jwtTokenGenerator.GenerateToken(Arg.Any<IUser>()).Returns("new-access-token");
        _refreshTokenGenerator.Generate().Returns("new-raw-refresh");
        _refreshTokenGenerator.Hash("new-raw-refresh").Returns("new-refresh-hash");
        _refreshTokenRepository.CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<RefreshTokenEntity>());

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-raw-refresh");
        result.ExpiresIn.Should().Be(900);

        await _refreshTokenRepository.Received(1).RevokeAsync(stored, Arg.Any<CancellationToken>());
        await _refreshTokenRepository.Received(1).CreateAsync(
            Arg.Is<RefreshTokenEntity>(rt => rt.UserId == user.Id && rt.TokenHash == "new-refresh-hash"),
            Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An unknown refresh token returns the generic unauthorized error")]
    public async Task Given_UnknownToken_When_Handling_Then_ThrowsUnauthorized()
    {
        _refreshTokenGenerator.Hash(Arg.Any<string>()).Returns("some-hash");
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshTokenEntity?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An expired refresh token returns 401 without dropping other sessions")]
    public async Task Given_ExpiredToken_When_Handling_Then_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var expired = new RefreshTokenEntity(userId, "stored-token-hash", DateTime.UtcNow.AddSeconds(-1));

        _refreshTokenGenerator.Hash("raw-token").Returns("stored-token-hash");
        _refreshTokenRepository.GetByTokenHashAsync("stored-token-hash", Arg.Any<CancellationToken>()).Returns(expired);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Reusing a revoked refresh token revokes every session for the user and returns 401")]
    public async Task Given_RevokedToken_When_Reused_Then_RevokesAllAndThrows()
    {
        var userId = Guid.NewGuid();
        var revoked = new RefreshTokenEntity(userId, "stored-token-hash", DateTime.UtcNow.AddDays(7));
        revoked.Revoke();

        _refreshTokenGenerator.Hash("raw-token").Returns("stored-token-hash");
        _refreshTokenRepository.GetByTokenHashAsync("stored-token-hash", Arg.Any<CancellationToken>()).Returns(revoked);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _refreshTokenRepository.Received(1).RevokeAllByUserAsync(userId, Arg.Any<CancellationToken>());
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<IUser>());
        await _refreshTokenRepository.DidNotReceive().CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }
}
