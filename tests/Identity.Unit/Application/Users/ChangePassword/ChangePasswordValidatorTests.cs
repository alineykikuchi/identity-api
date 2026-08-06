using FluentAssertions;
using Identity.Application.Users.ChangePassword;
using Identity.Domain.Validation;

namespace Identity.Unit.Application.Users.ChangePassword;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    private static ChangePasswordCommand Command(string currentPassword, string newPassword) => new()
    {
        UserId = Guid.NewGuid(),
        CurrentPassword = currentPassword,
        NewPassword = newPassword
    };

    [Fact(DisplayName = "A distinct new password that meets the policy is accepted")]
    public void Given_ValidPasswords_When_Validating_Then_IsValid()
    {
        var result = _validator.Validate(Command("current-password", "new-password"));

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "A new password shorter than the policy minimum is rejected")]
    public void Given_ShortNewPassword_When_Validating_Then_IsInvalid()
    {
        var shortPassword = new string('a', PasswordPolicy.MinimumLength - 1);

        var result = _validator.Validate(Command("current-password", shortPassword));

        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "A new password equal to the current one is rejected")]
    public void Given_UnchangedPassword_When_Validating_Then_IsInvalid()
    {
        var result = _validator.Validate(Command("same-password", "same-password"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact(DisplayName = "A missing current password is rejected")]
    public void Given_EmptyCurrentPassword_When_Validating_Then_IsInvalid()
    {
        var result = _validator.Validate(Command(string.Empty, "new-password"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
    }
}
