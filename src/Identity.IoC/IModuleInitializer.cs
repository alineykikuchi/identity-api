using Microsoft.AspNetCore.Builder;

namespace Identity.IoC;

/// <summary>
/// Contract for module initializers, each one responsible for registering the
/// dependencies of its own layer.
/// </summary>
public interface IModuleInitializer
{
    void Initialize(WebApplicationBuilder builder);
}
