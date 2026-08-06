using MediatR;

namespace Identity.Application.Users.UpdateUser;

/// <summary>
/// Command to update the profile of the authenticated user. The email is not editable:
/// changing it requires a verification flow that does not exist yet.
/// </summary>
public class UpdateUserCommand : IRequest<UpdateUserResult>
{
    /// <summary>Identifier of the authenticated user, taken from the access token claims.</summary>
    public Guid UserId { get; set; }

    /// <summary>New display name.</summary>
    public string Name { get; set; } = string.Empty;
}
