using FluentAssertions;
using Identity.Application.Users.UpdateUser;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using NSubstitute;

namespace Identity.Unit.Application.Users.UpdateUser;

public class UpdateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerTests()
    {
        _handler = new UpdateUserHandler(_userRepository);
    }

    private static UpdateUserCommand Command(Guid userId, string name = "New Name") =>
        new() { UserId = userId, Name = name };

    [Fact(DisplayName = "The display name of an active user is updated and persisted")]
    public async Task Given_ActiveUser_When_Handling_Then_UpdatesName()
    {
        var user = new User("person@example.com", "stored-hash", "Old Name");
        var updatedAtBefore = user.UpdatedAt;

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        var result = await _handler.Handle(Command(user.Id), CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be("person@example.com");
        result.Name.Should().Be("New Name");

        user.Name.Should().Be("New Name");
        user.UpdatedAt.Should().BeOnOrAfter(updatedAtBefore);
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A user that no longer exists cannot be updated")]
    public async Task Given_UnknownUser_When_Handling_Then_ThrowsUnauthorized()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A deactivated user cannot be updated")]
    public async Task Given_InactiveUser_When_Handling_Then_ThrowsUnauthorized()
    {
        var user = new User("person@example.com", "stored-hash");
        user.Deactivate();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _handler.Handle(Command(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
