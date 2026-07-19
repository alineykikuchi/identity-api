using Identity.IoC.ModuleInitializers;
using Microsoft.AspNetCore.Builder;

namespace Identity.IoC;

/// <summary>
/// Single composition point: triggers the initializer of every module.
/// </summary>
public static class DependencyResolver
{
    public static void RegisterDependencies(this WebApplicationBuilder builder)
    {
        new ApplicationModuleInitializer().Initialize(builder);
        new InfrastructureModuleInitializer().Initialize(builder);
        new WebApiModuleInitializer().Initialize(builder);
    }
}
