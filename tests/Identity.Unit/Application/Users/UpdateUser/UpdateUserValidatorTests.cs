using FluentAssertions;
using Identity.Application.Users.UpdateUser;

namespace Identity.Unit.Application.Users.UpdateUser;

public class UpdateUserValidatorTests
{
    private readonly UpdateUserValidator _validator = new();

    private static UpdateUserCommand Command(string name) => new() { UserId = Guid.NewGuid(), Name = name };

    [Fact(DisplayName = "A non-empty name is accepted")]
    public void Given_ValidName_When_Validating_Then_IsValid()
    {
        var result = _validator.Validate(Command("Person"));

        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "A blank name is rejected")]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_BlankName_When_Validating_Then_IsInvalid(string name)
    {
        var result = _validator.Validate(Command(name));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateUserCommand.Name));
    }

    [Fact(DisplayName = "A name longer than the mapped column is rejected")]
    public void Given_TooLongName_When_Validating_Then_IsInvalid()
    {
        var result = _validator.Validate(Command(new string('a', UpdateUserValidator.MaximumNameLength + 1)));

        result.IsValid.Should().BeFalse();
    }
}
