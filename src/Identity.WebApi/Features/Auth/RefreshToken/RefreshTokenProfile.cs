using AutoMapper;
using Identity.Application.Auth.RefreshToken;

namespace Identity.WebApi.Features.Auth.RefreshToken;

/// <summary>
/// AutoMapper profile for the refresh feature: maps the API request to the command and
/// the use-case result to the API response.
/// </summary>
public class RefreshTokenProfile : Profile
{
    public RefreshTokenProfile()
    {
        CreateMap<RefreshTokenRequest, RefreshTokenCommand>();
        CreateMap<RefreshTokenResult, RefreshTokenResponse>();
    }
}
