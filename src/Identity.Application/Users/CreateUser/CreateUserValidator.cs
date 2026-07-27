using FluentValidation;
using Identity.Domain.Validation;

namespace Identity.Application.Users.CreateUser;

/// <summary>
/// Validates a <see cref="CreateUserCommand"/>. Runs automatically through the MediatR
/// <c>ValidationBehavior</c> pipeline before the handler executes.
/// </summary>
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(command => command.Password)
            .SetValidator(new PasswordValidator());
    }
}
