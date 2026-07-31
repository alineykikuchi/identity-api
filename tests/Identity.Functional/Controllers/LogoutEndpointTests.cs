using System.Net;
using System.Net.Http.Json;
using Identity.Functional.Infrastructure;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;

namespace Identity.Functional.Controllers;

public class LogoutEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public LogoutEndpointTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "After logout the refresh token no longer works on /api/refresh")]
    public async Task Given_LoggedInUser_When_Logout_Then_RefreshTokenStopsWorking()
    {
        var login = await RegisterAndLoginAsync();

        var logout = await _client.PostAsJsonAsync("/api/logout", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _client.PostAsJsonAsync("/api/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact(DisplayName = "Logout with an unknown token returns 204")]
    public async Task Given_UnknownToken_When_Logout_Then_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/api/logout",
            new { refreshToken = $"not-a-real-token-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "Logging out twice with the same token returns 204 both times")]
    public async Task Given_AlreadyLoggedOutToken_When_LogoutAgain_Then_Returns204()
    {
        var login = await RegisterAndLoginAsync();

        var first = await _client.PostAsJsonAsync("/api/logout", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/logout", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    private async Task<LoginResponse> RegisterAndLoginAsync()
    {
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        const string password = "password123";

        var register = await _client.PostAsJsonAsync("/api/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();
        return body!.Data!;
    }
}
