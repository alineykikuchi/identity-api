using FluentValidation;
using Identity.Domain.Entities;

namespace Identity.Domain.Validation;

/// <summary>
/// Validates the invariants of a <see cref="User"/> entity. Password strength is a
/// plaintext concern and is validated separately by <see cref="PasswordValidator"/>.
/// </summary>
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(user => user.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
    }
}
