using Identity.Common.Validation;

namespace Identity.WebApi.Common;

/// <summary>
/// Standard API response envelope.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<ValidationErrorDetail> Errors { get; set; } = [];
}
