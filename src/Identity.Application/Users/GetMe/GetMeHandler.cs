using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Users.GetMe;

/// <summary>
/// Returns the profile of the authenticated user.
/// </summary>
/// <remarks>
/// The token only proves who the caller was when it was issued, so the database stays the
/// source of truth: an account deleted or deactivated after the token was issued is
/// rejected even though the signature is still valid.
/// </remarks>
public class GetMeHandler : IRequestHandler<GetMeQuery, GetMeResult>
{
    private const string InvalidUser = "Unauthorized.";

    private readonly IUserRepository _userRepository;

    public GetMeHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetMeResult> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException(InvalidUser);
        }

        return new GetMeResult
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name
        };
    }
}
