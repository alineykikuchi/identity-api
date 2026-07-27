using AutoMapper;
using Identity.Application.Users.CreateUser;

namespace Identity.WebApi.Features.Users.CreateUser;

/// <summary>
/// AutoMapper profile for the register feature: maps the API request to the command
/// and the use-case result to the API response.
/// </summary>
public class CreateUserProfile : Profile
{
    public CreateUserProfile()
    {
        CreateMap<CreateUserRequest, CreateUserCommand>();
        CreateMap<CreateUserResult, CreateUserResponse>();
    }
}
