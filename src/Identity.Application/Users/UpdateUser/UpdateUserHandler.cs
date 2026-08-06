using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Users.UpdateUser;

/// <summary>
/// Updates the profile of the authenticated user. Input is validated upstream by the pipeline.
/// </summary>
/// <remarks>
/// The user is loaded from the database rather than taken from the token, so an account
/// deleted or deactivated after the token was issued cannot be edited.
/// </remarks>
public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UpdateUserResult>
{
    private const string InvalidUser = "Unauthorized.";

    private readonly IUserRepository _userRepository;

    public UpdateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UpdateUserResult> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException(InvalidUser);
        }

        user.UpdateName(command.Name);

        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);

        return new UpdateUserResult
        {
            Id = updatedUser.Id,
            Email = updatedUser.Email,
            Name = updatedUser.Name
        };
    }
}
