using Identity.ORM;
using Identity.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Identity.Functional.Infrastructure;

/// <summary>
/// Boots the WebApi in-memory against a throwaway PostgreSQL container. The container
/// connection string (and a test JWT key) are injected into configuration, and the
/// EF Core migrations are applied before the tests run.
/// </summary>
public class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Signing key used by the test host. Exposed so tests can craft tokens the API
    /// accepts as genuine (an expired one, for instance).
    /// </summary>
    public const string JwtSecretKey = "FunctionalTestsOnlySecretKeyThatIsLongEnoughForHmacSha256";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:13")
        .WithDatabase("identity_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public IdentityApiFactory()
    {
        // The key goes through an environment variable rather than ConfigureAppConfiguration
        // below: AddJwtAuthentication reads Jwt:SecretKey while Program.Main is still
        // running, which is before the test host's configuration overrides are applied.
        // Environment variables are already part of the configuration at that point, so
        // the token validation parameters and the token generator end up on the same key.
        Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtSecretKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Read lazily when the DbContext options are built, so applying it here is fine.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _database.GetConnectionString()
            });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _database.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
