using FluentAssertions;
using Identity.Domain.Validation;

namespace Identity.Unit.Domain.Validation;

public class PasswordValidatorTests
{
    private readonly PasswordValidator _validator = new();

    [Fact(DisplayName = "A password meeting the minimum length passes validation")]
    public void Given_LongEnoughPassword_When_Validating_Then_ShouldBeValid()
    {
        var password = new string('x', PasswordPolicy.MinimumLength);

        var result = _validator.Validate(password);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "A password shorter than the minimum length fails validation")]
    public void Given_ShortPassword_When_Validating_Then_ShouldBeInvalid()
    {
        var password = new string('x', PasswordPolicy.MinimumLength - 1);

        var result = _validator.Validate(password);

        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "An empty password fails validation")]
    public void Given_EmptyPassword_When_Validating_Then_ShouldBeInvalid()
    {
        var result = _validator.Validate(string.Empty);

        result.IsValid.Should().BeFalse();
    }
}
