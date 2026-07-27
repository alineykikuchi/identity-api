using FluentAssertions;
using Identity.Application.Users.CreateUser;

namespace Identity.Unit.Application.Users.CreateUser;

public class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    [Fact(DisplayName = "A command with a valid email and password passes validation")]
    public void Given_ValidCommand_When_Validating_Then_ShouldBeValid()
    {
        var command = new CreateUserCommand { Email = "person@example.com", Password = "password123" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "A command with a malformed email fails validation")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("")]
    public void Given_InvalidEmail_When_Validating_Then_ShouldBeInvalid(string email)
    {
        var command = new CreateUserCommand { Email = email, Password = "password123" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact(DisplayName = "A command with a password shorter than 8 characters fails validation")]
    public void Given_ShortPassword_When_Validating_Then_ShouldBeInvalid()
    {
        var command = new CreateUserCommand { Email = "person@example.com", Password = "short" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }
}
