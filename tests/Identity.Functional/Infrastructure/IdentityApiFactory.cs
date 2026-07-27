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
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:13")
        .WithDatabase("identity_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _database.GetConnectionString(),
                ["Jwt:SecretKey"] = "FunctionalTestsOnlySecretKeyThatIsLongEnoughForHmacSha256"
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
