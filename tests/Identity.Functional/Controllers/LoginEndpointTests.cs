using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Identity.Functional.Infrastructure;
using Identity.ORM;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;
using Identity.WebApi.Features.Users.CreateUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Functional.Controllers;

public class LoginEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;
    private readonly HttpClient _client;

    public LoginEndpointTests(IdentityApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "Login after registration returns tokens; the access token carries the user's claims")]
    public async Task Given_RegisteredUser_When_Login_Then_ReturnsValidTokens()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        var userId = await RegisterAsync(email, "password123");

        var response = await _client.PostAsJsonAsync("/api/login", new { email, password = "password123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);

        var data = body.Data;
        Assert.False(string.IsNullOrWhiteSpace(data.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(data.RefreshToken));
        Assert.Equal(900, data.ExpiresIn);

        // Decode the access token and confirm it carries the user's id and email.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(data.AccessToken);
        Assert.Contains(jwt.Claims, c => c.Value == userId.ToString());
        Assert.Contains(jwt.Claims, c => c.Value == email);

        // The database must store the refresh token as a hash, never in plaintext.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        var stored = await context.RefreshTokens.SingleAsync(t => t.UserId == userId);
        Assert.NotEqual(data.RefreshToken, stored.TokenHash);
        Assert.False(string.IsNullOrWhiteSpace(stored.TokenHash));
    }

    [Fact(DisplayName = "Login with a wrong password returns 401")]
    public async Task Given_WrongPassword_When_Login_Then_Returns401()
    {
        var email = $"wrong-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email, "password123");

        var response = await _client.PostAsJsonAsync("/api/login", new { email, password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Login with an unknown email returns 401")]
    public async Task Given_UnknownEmail_When_Login_Then_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/login",
            new { email = $"ghost-{Guid.NewGuid():N}@example.com", password = "password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> RegisterAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<CreateUserResponse>>();
        return body!.Data!.Id;
    }
}
