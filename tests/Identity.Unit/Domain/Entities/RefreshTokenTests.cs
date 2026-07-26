using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.Unit.Domain.Entities;

public class RefreshTokenTests
{
    [Fact(DisplayName = "A non-revoked token with a future expiry is active")]
    public void Given_FutureExpiryAndNotRevoked_When_CheckingIsActive_Then_ShouldBeTrue()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));

        token.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "A token past its expiry is not active")]
    public void Given_PastExpiry_When_CheckingIsActive_Then_ShouldBeFalse()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddSeconds(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact(DisplayName = "A revoked token is not active even before its expiry")]
    public void Given_RevokedToken_When_CheckingIsActive_Then_ShouldBeFalse()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));

        token.Revoke();

        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
    }
}
