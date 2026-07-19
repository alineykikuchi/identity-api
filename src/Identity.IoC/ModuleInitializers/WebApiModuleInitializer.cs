using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.IoC.ModuleInitializers;

/// <summary>
/// Registers the dependencies of the driver adapter (WebApi).
/// </summary>
public class WebApiModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddHealthChecks();
    }
}
