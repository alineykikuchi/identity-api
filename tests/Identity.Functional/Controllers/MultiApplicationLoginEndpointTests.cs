using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Identity.Domain.Entities;
using Identity.Functional.Infrastructure;
using Identity.ORM;
using Identity.ORM.Mapping;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;
using Identity.WebApi.Features.Auth.RefreshToken;
using Identity.WebApi.Features.Users.CreateUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ApplicationEntity = Identity.Domain.Entities.Application;

namespace Identity.Functional.Controllers;

/// <summary>
/// End-to-end coverage of per-application roles: the user is global, the roles are not.
/// </summary>
public class MultiApplicationLoginEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;
    private readonly HttpClient _client;

    public MultiApplicationLoginEndpointTests(IdentityApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "The same user logging into two applications gets a different audience and different roles in each")]
    public async Task Given_UserWithRolesInTwoApplications_When_Login_Then_TokensDifferPerClientId()
    {
        var email = $"multi-{Guid.NewGuid():N}@example.com";
        var userId = await RegisterAsync(email, "password123");

        var storeClientId = $"store-{Guid.NewGuid():N}";
        var portalClientId = $"portal-{Guid.NewGuid():N}";

        // Same user, deliberately different roles in each application.
        await SeedApplicationAsync(storeClientId, userId, "Admin", "Customer");
        await SeedApplicationAsync(portalClientId, userId, "Viewer");

        var storeToken = await LoginAsync(email, "password123", storeClientId);
        var portalToken = await LoginAsync(email, "password123", portalClientId);

        var storeJwt = Read(storeToken);
        var portalJwt = Read(portalToken);

        Assert.Equal(new[] { storeClientId }, storeJwt.Audiences);
        Assert.Equal(new[] { portalClientId }, portalJwt.Audiences);

        Assert.Equal(new[] { "Admin", "Customer" }, RolesOf(storeJwt).OrderBy(r => r));
        Assert.Equal(new[] { "Viewer" }, RolesOf(portalJwt));

        // The decisive assertion: neither application's roles leak into the other's token.
        Assert.DoesNotContain("Viewer", RolesOf(storeJwt));
        Assert.DoesNotContain("Admin", RolesOf(portalJwt));
        Assert.DoesNotContain("Customer", RolesOf(portalJwt));

        // Both tokens still belong to the same global user.
        Assert.Contains(storeJwt.Claims, c => c.Value == userId.ToString());
        Assert.Contains(portalJwt.Claims, c => c.Value == userId.ToString());
    }

    [Fact(DisplayName = "Logging in without a client id keeps the previous behaviour: no audience, no roles")]
    public async Task Given_NoClientId_When_Login_Then_TokenHasNoAudienceOrRoles()
    {
        var email = $"noclient-{Guid.NewGuid():N}@example.com";
        var userId = await RegisterAsync(email, "password123");

        var clientId = $"store-{Guid.NewGuid():N}";
        await SeedApplicationAsync(clientId, userId, "Admin");

        var jwt = Read(await LoginAsync(email, "password123", clientId: null));

        Assert.Empty(jwt.Audiences);
        Assert.Empty(RolesOf(jwt));
    }

    [Fact(DisplayName = "A user with no roles in the application still logs in, with a token carrying no role claim")]
    public async Task Given_UserWithoutRoles_When_Login_Then_SucceedsWithoutRoleClaims()
    {
        var email = $"noroles-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email, "password123");

        // The application exists, but the user holds no role in it.
        var clientId = $"empty-{Guid.NewGuid():N}";
        await SeedApplicationAsync(clientId, userId: null);

        var jwt = Read(await LoginAsync(email, "password123", clientId));

        Assert.Equal(new[] { clientId }, jwt.Audiences);
        Assert.Empty(RolesOf(jwt));
    }

    [Fact(DisplayName = "Logging in with an unknown client id returns 401")]
    public async Task Given_UnknownClientId_When_Login_Then_Returns401()
    {
        var email = $"ghostapp-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email, "password123");

        var response = await _client.PostAsJsonAsync("/api/login",
            new { email, password = "password123", clientId = $"ghost-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Logging in against a deactivated application returns 401")]
    public async Task Given_InactiveApplication_When_Login_Then_Returns401()
    {
        var email = $"inactiveapp-{Guid.NewGuid():N}@example.com";
        var userId = await RegisterAsync(email, "password123");

        var clientId = $"retired-{Guid.NewGuid():N}";
        await SeedApplicationAsync(clientId, userId, "Admin");
        await DeactivateApplicationAsync(clientId);

        var response = await _client.PostAsJsonAsync("/api/login",
            new { email, password = "password123", clientId });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Refreshing an application-scoped token keeps the same audience and roles")]
    public async Task Given_ApplicationScopedLogin_When_Refresh_Then_KeepsAudienceAndRoles()
    {
        var email = $"refreshapp-{Guid.NewGuid():N}@example.com";
        var userId = await RegisterAsync(email, "password123");

        var clientId = $"store-{Guid.NewGuid():N}";
        await SeedApplicationAsync(clientId, userId, "Admin");

        var loginResponse = await _client.PostAsJsonAsync("/api/login",
            new { email, password = "password123", clientId });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var login = await loginResponse.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();
        var refreshResponse = await _client.PostAsJsonAsync("/api/refresh",
            new { refreshToken = login!.Data!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<ApiResponseWithData<RefreshTokenResponse>>();
        var jwt = Read(refreshed!.Data!.AccessToken);

        Assert.Equal(new[] { clientId }, jwt.Audiences);
        Assert.Equal(new[] { "Admin" }, RolesOf(jwt));
    }

    [Fact(DisplayName = "The migrations seed the sample application with its Admin and Customer roles")]
    public async Task Given_MigratedDatabase_When_Querying_Then_SampleApplicationIsSeeded()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        var application = await context.Applications
            .SingleAsync(a => a.ClientId == DevelopmentSeed.ApplicationClientId);
        Assert.True(application.IsActive);

        var roles = await context.Roles
            .Where(r => r.ApplicationId == application.Id)
            .Select(r => r.Name)
            .OrderBy(name => name)
            .ToListAsync();

        Assert.Equal(new[] { "Admin", "Customer" }, roles);
    }

    [Fact(DisplayName = "The migrations seed the party-invitation application, active and with no roles")]
    public async Task Given_MigratedDatabase_When_Querying_Then_PartyPlatformApplicationIsSeeded()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        var application = await context.Applications
            .SingleAsync(a => a.ClientId == DevelopmentSeed.PartyPlatformClientId);

        Assert.True(application.IsActive);

        // That application has no role-based authorization yet, so seeding roles for it
        // would only add rows nothing reads.
        var roles = await context.Roles.Where(r => r.ApplicationId == application.Id).ToListAsync();
        Assert.Empty(roles);
    }

    [Fact(DisplayName = "A user with no roles signs in to the party-invitation application and gets its audience")]
    public async Task Given_PartyPlatformClientId_When_LoggingIn_Then_TokenCarriesItsAudience()
    {
        const string email = "organizer@example.com";
        const string password = "password123";
        await RegisterAsync(email, password);

        var jwt = Read(await LoginAsync(email, password, DevelopmentSeed.PartyPlatformClientId));

        Assert.Equal([DevelopmentSeed.PartyPlatformClientId], jwt.Audiences.ToArray());
        Assert.Empty(RolesOf(jwt));
    }

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static string[] RolesOf(JwtSecurityToken jwt) =>
        jwt.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

    private async Task<Guid> RegisterAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<CreateUserResponse>>();
        return body!.Data!.Id;
    }

    private async Task<string> LoginAsync(string email, string password, string? clientId)
    {
        var response = await _client.PostAsJsonAsync("/api/login", new { email, password, clientId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<LoginResponse>>();
        return body!.Data!.AccessToken;
    }

    /// <summary>
    /// Registers an application with the given roles and, when a user is supplied, assigns
    /// every one of those roles to them.
    /// </summary>
    private async Task SeedApplicationAsync(string clientId, Guid? userId, params string[] roleNames)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        var application = new ApplicationEntity(clientId, clientId);
        context.Applications.Add(application);

        foreach (var roleName in roleNames)
        {
            var role = new Role(application.Id, roleName);
            context.Roles.Add(role);

            if (userId is not null)
            {
                context.UserRoles.Add(new UserRole(userId.Value, role.Id));
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task DeactivateApplicationAsync(string clientId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        var application = await context.Applications.SingleAsync(a => a.ClientId == clientId);
        application.Deactivate();
        await context.SaveChangesAsync();
    }
}
