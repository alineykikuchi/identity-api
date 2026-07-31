using System.Net;
using System.Net.Http.Json;
using Identity.Functional.Infrastructure;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;
using Identity.WebApi.Features.Auth.RefreshToken;

namespace Identity.Functional.Controllers;

public class RefreshEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public RefreshEndpointTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "Refresh returns a new token pair and rotates the refresh token")]
    public async Task Given_ValidRefreshToken_When_Refresh_Then_ReturnsNewPair()
    {
        var login = await RegisterAndLoginAsync();

        var response = await _client.PostAsJsonAsync("/api/refresh", new { refreshToken = login.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<RefreshTokenResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);

        var data = body.Data;
        Assert.False(string.IsNullOrWhiteSpace(data.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(data.RefreshToken));
        Assert.NotEqual(login.RefreshToken, data.RefreshToken);
        Assert.Equal(900, data.ExpiresIn);
    }

    [Fact(DisplayName = "The old refresh token stops working after it has been rotated")]
    public async Task Given_RotatedRefreshToken_When_ReusingTheOldOne_Then_Returns401()
    {
        var login = await RegisterAndLoginAsync();

        var first = await _client.PostAsJsonAsync("/api/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var reuse = await _client.PostAsJsonAsync("/api/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact(DisplayName = "Refresh with an unknown token returns 401")]
    public async Task Given_UnknownToken_When_Refresh_Then_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/refresh",
            new { refreshToken = $"not-a-real-token-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<LoginResponse> RegisterAndLoginAsync()
    {
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        const string password = "password123";

        var register = await _client.PostAsJsonAsync("/api/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();
        return body!.Data!;
    }
}
