namespace Identity.WebApi.Features.Users.UpdateUser;

/// <summary>
/// Request body for <c>PUT /api/me</c>. The email is not editable for now.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>New display name.</summary>
    public string Name { get; set; } = string.Empty;
}
