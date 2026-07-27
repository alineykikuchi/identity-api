using System.Net;
using System.Net.Http.Json;
using Identity.Functional.Infrastructure;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Users.CreateUser;
using Xunit;

namespace Identity.Functional.Controllers;

public class RegisterEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public RegisterEndpointTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "POST /api/register with valid data returns 201 and the created user")]
    public async Task Given_ValidRequest_When_Register_Then_Returns201()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/register", new { email, password = "password123" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<CreateUserResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(email, body.Data!.Email);
        Assert.NotEqual(Guid.Empty, body.Data.Id);
    }

    [Theory(DisplayName = "POST /api/register with invalid data returns 400")]
    [InlineData("not-an-email", "password123")]
    [InlineData("valid@example.com", "short")]
    public async Task Given_InvalidRequest_When_Register_Then_Returns400(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/register with an already-registered email returns 409")]
    public async Task Given_DuplicateEmail_When_Register_Then_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";

        var first = await _client.PostAsJsonAsync("/api/register", new { email, password = "password123" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/register", new { email, password = "password123" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
