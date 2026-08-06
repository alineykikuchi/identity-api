using AutoMapper;
using Identity.Application.Users.UpdateUser;

namespace Identity.WebApi.Features.Users.UpdateUser;

/// <summary>
/// AutoMapper profile for the profile-update feature: maps the API request to the command
/// and the use-case result to the API response. The user id is not part of the request —
/// it comes from the token claims and is set by the controller.
/// </summary>
public class UpdateUserProfile : Profile
{
    public UpdateUserProfile()
    {
        CreateMap<UpdateUserRequest, UpdateUserCommand>();
        CreateMap<UpdateUserResult, UpdateUserResponse>();
    }
}
