using AutoMapper;
using Identity.Application.Auth.Logout;

namespace Identity.WebApi.Features.Auth.Logout;

/// <summary>
/// AutoMapper profile for the logout feature: maps the API request to the command.
/// The use case returns no data, so there is no response mapping.
/// </summary>
public class LogoutProfile : Profile
{
    public LogoutProfile()
    {
        CreateMap<LogoutRequest, LogoutCommand>();
    }
}
