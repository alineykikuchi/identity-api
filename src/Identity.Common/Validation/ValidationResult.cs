using FluentValidation.Results;

namespace Identity.Common.Validation;

/// <summary>
/// Result of a validation, with the errors already converted to <see cref="ValidationErrorDetail"/>.
/// </summary>
public class ValidationResultDetail
{
    public bool IsValid { get; set; }
    public IEnumerable<ValidationErrorDetail> Errors { get; set; } = [];

    public ValidationResultDetail()
    {
    }

    public ValidationResultDetail(ValidationResult validationResult)
    {
        IsValid = validationResult.IsValid;
        Errors = validationResult.Errors.Select(o => (ValidationErrorDetail)o);
    }
}
