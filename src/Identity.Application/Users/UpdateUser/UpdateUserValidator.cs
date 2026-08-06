using FluentValidation;

namespace Identity.Application.Users.UpdateUser;

/// <summary>
/// Validates an <see cref="UpdateUserCommand"/>. Runs automatically through the MediatR
/// <c>ValidationBehavior</c> pipeline before the handler executes.
/// </summary>
public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>Maximum name length, matching the column mapped in the persistence layer.</summary>
    public const int MaximumNameLength = 256;

    public UpdateUserValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"Name must be at most {MaximumNameLength} characters long.");
    }
}
