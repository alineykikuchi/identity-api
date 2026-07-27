using FluentAssertions;
using Identity.Application.Users.CreateUser;
using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using NSubstitute;

namespace Identity.Unit.Application.Users.CreateUser;

public class CreateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _handler = new CreateUserHandler(_userRepository, _passwordHasher);
    }

    [Fact(DisplayName = "Registering a new email hashes the password and persists an active user")]
    public async Task Given_NewEmail_When_Handling_Then_CreatesHashedUser()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.HashPassword("password123").Returns("hashed-password");
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        var command = new CreateUserCommand { Email = "Person@Example.com", Password = "password123" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be("person@example.com");
        result.Id.Should().NotBe(Guid.Empty);

        _passwordHasher.Received(1).HashPassword("password123");
        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Email == "person@example.com"
                && u.PasswordHash == "hashed-password"
                && u.Status == Identity.Domain.Enums.UserStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Registering an existing email throws a conflict and does not persist")]
    public async Task Given_ExistingEmail_When_Handling_Then_ThrowsConflict()
    {
        var existing = new User("person@example.com", "existing-hash");
        _userRepository.GetByEmailAsync("person@example.com", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new CreateUserCommand { Email = "Person@Example.com", Password = "password123" };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
