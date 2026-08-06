using FluentValidation;
using Identity.Domain.Validation;

namespace Identity.Application.Users.ChangePassword;

/// <summary>
/// Validates a <see cref="ChangePasswordCommand"/>. Runs automatically through the MediatR
/// <c>ValidationBehavior</c> pipeline before the handler executes.
/// </summary>
/// <remarks>
/// Only the shape of the request is checked here. Whether the current password is actually
/// correct is decided by the handler against the stored hash, and answered with the same
/// generic unauthorized error.
/// </remarks>
public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(command => command.NewPassword)
            .SetValidator(new PasswordValidator());

        RuleFor(command => command.NewPassword)
            .NotEqual(command => command.CurrentPassword)
                .WithMessage("New password must be different from the current password.");
    }
}
