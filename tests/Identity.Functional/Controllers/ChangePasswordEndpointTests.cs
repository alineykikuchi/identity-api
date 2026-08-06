using Identity.Functional.Infrastructure;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Identity.Functional.Controllers;

public class ChangePasswordEndpointTests : IClassFixture<IdentityApiFactory>
{
    private const string CurrentPassword = "password123";
    private const string NewPassword = "new-password456";

    private readonly HttpClient _client;

    public ChangePasswordEndpointTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "Changing the password drops existing sessions and the new password works")]
    public async Task Given_CorrectCurrentPassword_When_ChangingPassword_Then_SessionsDropAndNewPasswordWorks()
    {
        var (email, login) = await RegisterAndLoginAsync();

        var change = await ChangePasswordAsync(login.AccessToken, CurrentPassword, NewPassword);
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        // The refresh token issued before the change no longer buys a new access token.
        var refresh = await _client.PostAsJsonAsync("/api/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        var withOldPassword = await _client.PostAsJsonAsync("/api/login", new { email, password = CurrentPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, withOldPassword.StatusCode);

        var withNewPassword = await _client.PostAsJsonAsync("/api/login", new { email, password = NewPassword });
        Assert.Equal(HttpStatusCode.OK, withNewPassword.StatusCode);
    }

    [Fact(DisplayName = "A wrong current password returns 401 and keeps the password unchanged")]
    public async Task Given_WrongCurrentPassword_When_ChangingPassword_Then_Returns401()
    {
        var (email, login) = await RegisterAndLoginAsync();

        var change = await ChangePasswordAsync(login.AccessToken, "not-the-current-password", NewPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, change.StatusCode);

        var stillWorks = await _client.PostAsJsonAsync("/api/login", new { email, password = CurrentPassword });
        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    [Fact(DisplayName = "A new password that does not meet the policy is rejected with 400")]
    public async Task Given_ShortNewPassword_When_ChangingPassword_Then_Returns400()
    {
        var (_, login) = await RegisterAndLoginAsync();

        var change = await ChangePasswordAsync(login.AccessToken, CurrentPassword, "short");

        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
    }

    [Fact(DisplayName = "Changing the password without a token returns 401")]
    public async Task Given_NoToken_When_ChangingPassword_Then_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/me/password",
            new { currentPassword = CurrentPassword, newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(string Email, LoginResponse Login)> RegisterAndLoginAsync()
    {
        var email = $"change-password-{Guid.NewGuid():N}@example.com";

        var register = await _client.PostAsJsonAsync("/api/register", new { email, password = CurrentPassword });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/login", new { email, password = CurrentPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();
        return (email, body!.Data!);
    }

    private Task<HttpResponseMessage> ChangePasswordAsync(string accessToken, string currentPassword, string newPassword)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/me/password")
        {
            Content = JsonContent.Create(new { currentPassword, newPassword })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return _client.SendAsync(request);
    }
}
