using FluentValidation;
using Identity.Application.Users.CreateUser;
using Identity.Common.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.IoC.ModuleInitializers;

/// <summary>
/// Registers the dependencies of the application layer.
/// </summary>
public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

        // FluentValidation validators consumed by the MediatR ValidationBehavior pipeline.
        // Registered explicitly as features are added.
        builder.Services.AddScoped<IValidator<CreateUserCommand>, CreateUserValidator>();
    }
}
