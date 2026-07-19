using FluentValidation.Results;

namespace Identity.Common.Validation;

/// <summary>
/// Details of a single validation error, ready to be serialized in the API response.
/// </summary>
public class ValidationErrorDetail
{
    public string Error { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;

    public static explicit operator ValidationErrorDetail(ValidationFailure validationFailure)
    {
        return new ValidationErrorDetail
        {
            Detail = validationFailure.ErrorMessage,
            Error = validationFailure.ErrorCode
        };
    }
}
