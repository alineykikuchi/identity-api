using Identity.Domain.Repositories;
using Identity.ORM;
using Identity.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.IoC.ModuleInitializers;

/// <summary>
/// Registers the dependencies of the driven adapters (persistence).
/// Repositories are added here as they are created.
/// </summary>
public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }
}
