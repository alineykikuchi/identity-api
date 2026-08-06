using Identity.Functional.Infrastructure;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;
using Identity.WebApi.Features.Users.GetMe;
using Identity.WebApi.Features.Users.UpdateUser;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Identity.Functional.Controllers;

public class UpdateMeEndpointTests : IClassFixture<IdentityApiFactory>
{
    private const string Password = "password123";

    private readonly HttpClient _client;

    public UpdateMeEndpointTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "The authenticated user updates their own display name")]
    public async Task Given_ValidToken_When_UpdatingName_Then_NameIsPersisted()
    {
        var login = await RegisterAndLoginAsync();

        var response = await SendAsync(HttpMethod.Put, "/api/me", login.AccessToken, new { name = "Updated Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<UpdateUserResponse>>();
        Assert.NotNull(body?.Data);
        Assert.Equal("Updated Name", body.Data.Name);

        // Read it back to make sure it was persisted, not just echoed.
        var me = await SendAsync(HttpMethod.Get, "/api/me", login.AccessToken);
        var profile = await me.Content.ReadFromJsonAsync<ApiResponseWithData<GetMeResponse>>();
        Assert.Equal("Updated Name", profile?.Data?.Name);
    }

    [Fact(DisplayName = "Updating the profile without a token returns 401")]
    public async Task Given_NoToken_When_UpdatingName_Then_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/me", new { name = "Updated Name" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "An empty name is rejected with 400")]
    public async Task Given_EmptyName_When_UpdatingName_Then_Returns400()
    {
        var login = await RegisterAndLoginAsync();

        var response = await SendAsync(HttpMethod.Put, "/api/me", login.AccessToken, new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<LoginResponse> RegisterAndLoginAsync()
    {
        var email = $"update-me-{Guid.NewGuid():N}@example.com";

        var register = await _client.PostAsJsonAsync("/api/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();
        return body!.Data!;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string accessToken, object? payload = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return _client.SendAsync(request);
    }
}
