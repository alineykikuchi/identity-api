using FluentAssertions;
using Identity.Application.Users.GetMe;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using NSubstitute;

namespace Identity.Unit.Application.Users.GetMe;

public class GetMeHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetMeHandler _handler;

    public GetMeHandlerTests()
    {
        _handler = new GetMeHandler(_userRepository);
    }

    private static GetMeQuery Query(Guid userId) => new() { UserId = userId };

    [Fact(DisplayName = "An active user gets their own profile back")]
    public async Task Given_ActiveUser_When_Handling_Then_ReturnsProfile()
    {
        var user = new User("person@example.com", "stored-hash", "Person");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(Query(user.Id), CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be("person@example.com");
        result.Name.Should().Be("Person");
    }

    [Fact(DisplayName = "A user that no longer exists is unauthorized")]
    public async Task Given_UnknownUser_When_Handling_Then_ThrowsUnauthorized()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(Query(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact(DisplayName = "A user deactivated after the token was issued is unauthorized")]
    public async Task Given_InactiveUser_When_Handling_Then_ThrowsUnauthorized()
    {
        var user = new User("person@example.com", "stored-hash");
        user.Deactivate();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _handler.Handle(Query(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
