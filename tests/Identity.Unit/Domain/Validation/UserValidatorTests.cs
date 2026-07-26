using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Validation;

namespace Identity.Unit.Domain.Validation;

public class UserValidatorTests
{
    private readonly UserValidator _validator = new();

    [Fact(DisplayName = "A user with a well-formed email passes validation")]
    public void Given_ValidEmail_When_Validating_Then_ShouldBeValid()
    {
        var user = new User("person@example.com", "hashed-password");

        var result = _validator.Validate(user);

        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "A user with a malformed email fails validation")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Given_MalformedEmail_When_Validating_Then_ShouldBeInvalid(string email)
    {
        var user = new User(email, "hashed-password");

        var result = _validator.Validate(user);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(User.Email));
    }

    [Fact(DisplayName = "A user with an empty email fails validation")]
    public void Given_EmptyEmail_When_Validating_Then_ShouldBeInvalid()
    {
        var user = new User(string.Empty, "hashed-password");

        var result = _validator.Validate(user);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(User.Email));
    }
}
