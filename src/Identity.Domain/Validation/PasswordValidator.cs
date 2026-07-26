using FluentValidation;

namespace Identity.Domain.Validation;

/// <summary>
/// Central password policy. Single source of truth for password rules so use-case
/// validators (e.g. registration and password change) stay consistent.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>Minimum number of characters a password must contain.</summary>
    public const int MinimumLength = 8;
}

/// <summary>
/// Validates a plaintext password against <see cref="PasswordPolicy"/>. Applied before
/// hashing, so it runs on the plaintext value, never on the stored hash.
/// </summary>
public class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinimumLength)
                .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters long.");
    }
}
