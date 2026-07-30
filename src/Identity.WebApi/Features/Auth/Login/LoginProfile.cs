using AutoMapper;
using Identity.Application.Auth.Login;

namespace Identity.WebApi.Features.Auth.Login;

/// <summary>
/// AutoMapper profile for the login feature: maps the API request to the command and
/// the use-case result to the API response.
/// </summary>
public class LoginProfile : Profile
{
    public LoginProfile()
    {
        CreateMap<LoginRequest, LoginCommand>();
        CreateMap<LoginResult, LoginResponse>();
    }
}
