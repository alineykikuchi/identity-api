using FluentAssertions;
using Identity.Application.Users.ChangePassword;
using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using NSubstitute;

namespace Identity.Unit.Application.Users.ChangePassword;

public class ChangePasswordHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ChangePasswordHandler _handler;

    public ChangePasswordHandlerTests()
    {
        _handler = new ChangePasswordHandler(_userRepository, _refreshTokenRepository, _passwordHasher);
    }

    private static ChangePasswordCommand Command(Guid userId) => new()
    {
        UserId = userId,
        CurrentPassword = "current-password",
        NewPassword = "new-password"
    };

    [Fact(DisplayName = "The password is replaced and every session of the user is revoked")]
    public async Task Given_CorrectCurrentPassword_When_Handling_Then_ChangesPasswordAndRevokesSessions()
    {
        var user = new User("person@example.com", "old-hash");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("current-password", "old-hash").Returns(true);
        _passwordHasher.HashPassword("new-password").Returns("new-hash");
        _userRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        await _handler.Handle(Command(user.Id), CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _refreshTokenRepository.Received(1).RevokeAllByUserAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A wrong current password changes nothing and returns the generic unauthorized error")]
    public async Task Given_WrongCurrentPassword_When_Handling_Then_ThrowsUnauthorized()
    {
        var user = new User("person@example.com", "old-hash");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("current-password", "old-hash").Returns(false);

        var act = () => _handler.Handle(Command(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        user.PasswordHash.Should().Be("old-hash");
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A user that no longer exists cannot change a password")]
    public async Task Given_UnknownUser_When_Handling_Then_ThrowsUnauthorized()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<string>());
        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A deactivated user cannot change a password")]
    public async Task Given_InactiveUser_When_Handling_Then_ThrowsUnauthorized()
    {
        var user = new User("person@example.com", "old-hash");
        user.Deactivate();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _handler.Handle(Command(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<string>());
        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
