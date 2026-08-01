using Identity.Functional.Infrastructure;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;
using Identity.WebApi.Features.Users.CreateUser;
using Identity.WebApi.Features.Users.GetMe;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace Identity.Functional.Controllers;

public class MeEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public MeEndpointTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "A valid access token returns the authenticated user's profile")]
    public async Task Given_ValidToken_When_GetMe_Then_ReturnsProfile()
    {
        var (userId, email, accessToken) = await RegisterAndLoginAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<GetMeResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(userId, body.Data.Id);
        Assert.Equal(email, body.Data.Email);
    }

    [Fact(DisplayName = "A request without a token returns 401")]
    public async Task Given_NoToken_When_GetMe_Then_Returns401()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "An expired access token returns 401")]
    public async Task Given_ExpiredToken_When_GetMe_Then_Returns401()
    {
        var (userId, email, _) = await RegisterAndLoginAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ExpiredTokenFor(userId, email));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Builds a token correctly signed with the host's key but whose lifetime is already
    /// over, so only the expiration can be what the API rejects.
    /// </summary>
    private static string ExpiredTokenFor(Guid userId, string email)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(IdentityApiFactory.JwtSecretKey);
        var issuedAt = DateTime.UtcNow.AddMinutes(-30);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, email)
            ]),
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = DateTime.UtcNow.AddMinutes(-15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private async Task<(Guid UserId, string Email, string AccessToken)> RegisterAndLoginAsync()
    {
        var email = $"me-{Guid.NewGuid():N}@example.com";
        const string password = "password123";

        var register = await _client.PostAsJsonAsync("/api/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var registered = await register.Content.ReadFromJsonAsync<ApiResponseWithData<CreateUserResponse>>();

        var login = await _client.PostAsJsonAsync("/api/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var loggedIn = await login.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();

        return (registered!.Data!.Id, email, loggedIn!.Data!.AccessToken);
    }
}
