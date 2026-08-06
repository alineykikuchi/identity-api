using AutoMapper;
using Identity.Application.Users.ChangePassword;

namespace Identity.WebApi.Features.Users.ChangePassword;

/// <summary>
/// AutoMapper profile for the password-change feature: maps the API request to the command.
/// The use case returns no data, so there is no response mapping.
/// </summary>
public class ChangePasswordProfile : Profile
{
    public ChangePasswordProfile()
    {
        CreateMap<ChangePasswordRequest, ChangePasswordCommand>();
    }
}
