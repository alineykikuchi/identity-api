using AutoMapper;
using Identity.Application.Users.GetMe;

namespace Identity.WebApi.Features.Users.GetMe;

/// <summary>
/// AutoMapper profile for the "me" feature: maps the use-case result to the API response.
/// The query is built from the token claims, so there is no request mapping.
/// </summary>
public class GetMeProfile : Profile
{
    public GetMeProfile()
    {
        CreateMap<GetMeResult, GetMeResponse>();
    }
}
