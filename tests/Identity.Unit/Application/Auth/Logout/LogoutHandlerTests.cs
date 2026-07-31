using FluentAssertions;
using Identity.Application.Auth.Logout;
using Identity.Common.Security;
using Identity.Domain.Repositories;
using NSubstitute;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Unit.Application.Auth.Logout;

public class LogoutHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenGenerator _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _handler = new LogoutHandler(_refreshTokenRepository, _refreshTokenGenerator);
    }

    private static LogoutCommand Command(string token = "raw-token") => new() { RefreshToken = token };

    [Fact(DisplayName = "An active refresh token is revoked on logout")]
    public async Task Given_ActiveToken_When_Handling_Then_RevokesIt()
    {
        var stored = new RefreshTokenEntity(Guid.NewGuid(), "stored-token-hash", DateTime.UtcNow.AddDays(7));

        _refreshTokenGenerator.Hash("raw-token").Returns("stored-token-hash");
        _refreshTokenRepository.GetByTokenHashAsync("stored-token-hash", Arg.Any<CancellationToken>()).Returns(stored);

        await _handler.Handle(Command(), CancellationToken.None);

        await _refreshTokenRepository.Received(1).RevokeAsync(stored, Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An unknown refresh token is a no-op instead of an error")]
    public async Task Given_UnknownToken_When_Handling_Then_DoesNothing()
    {
        _refreshTokenGenerator.Hash(Arg.Any<string>()).Returns("some-hash");
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshTokenEntity?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _refreshTokenRepository.DidNotReceive().RevokeAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Logging out twice with the same token does not fail")]
    public async Task Given_AlreadyRevokedToken_When_Handling_Then_DoesNothing()
    {
        var revoked = new RefreshTokenEntity(Guid.NewGuid(), "stored-token-hash", DateTime.UtcNow.AddDays(7));
        revoked.Revoke();
        var revokedAt = revoked.RevokedAt;

        _refreshTokenGenerator.Hash("raw-token").Returns("stored-token-hash");
        _refreshTokenRepository.GetByTokenHashAsync("stored-token-hash", Arg.Any<CancellationToken>()).Returns(revoked);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _refreshTokenRepository.DidNotReceive().RevokeAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
        revoked.RevokedAt.Should().Be(revokedAt, "the first revocation time is preserved");
    }

    [Fact(DisplayName = "An expired refresh token is revoked without failing")]
    public async Task Given_ExpiredToken_When_Handling_Then_RevokesItWithoutThrowing()
    {
        var expired = new RefreshTokenEntity(Guid.NewGuid(), "stored-token-hash", DateTime.UtcNow.AddSeconds(-1));

        _refreshTokenGenerator.Hash("raw-token").Returns("stored-token-hash");
        _refreshTokenRepository.GetByTokenHashAsync("stored-token-hash", Arg.Any<CancellationToken>()).Returns(expired);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _refreshTokenRepository.Received(1).RevokeAsync(expired, Arg.Any<CancellationToken>());
    }
}
