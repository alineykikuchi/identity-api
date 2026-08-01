using MediatR;

namespace Identity.Application.Users.GetMe;

/// <summary>
/// Query for the profile of the currently authenticated user.
/// </summary>
public class GetMeQuery : IRequest<GetMeResult>
{
    /// <summary>Identifier of the authenticated user, taken from the access token claims.</summary>
    public Guid UserId { get; set; }
}
