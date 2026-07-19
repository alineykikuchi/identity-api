using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace Identity.Common.HealthChecks;

/// <summary>
/// Extension methods for configuring health checks in an ASP.NET Core application.
/// </summary>
public static class HealthChecksExtension
{
    /// <summary>
    /// Adds the basic health checks ("Liveness" and "Readiness") to the service collection.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    public static void AddBasicHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("Liveness", () => HealthCheckResult.Healthy(), tags: ["liveness"])
            .AddCheck("Readiness", () => HealthCheckResult.Healthy(), tags: ["readiness"]);
    }

    /// <summary>
    /// Exposes the <c>/health/live</c>, <c>/health/ready</c> and <c>/health</c> endpoints.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    public static void UseBasicHealthChecks(this WebApplication app)
    {
        var livenessOptions = WriteHealthCheckResponse(app, "liveness");
        app.UseHealthChecks("/health/live", livenessOptions);

        var readinessOptions = WriteHealthCheckResponse(app, "readiness");
        app.UseHealthChecks("/health/ready", readinessOptions);

        var healthOptions = WriteHealthCheckResponse(app, string.Empty);
        app.UseHealthChecks("/health", healthOptions);

        var logger = app.Services.GetRequiredService<ILogger<HealthCheckService>>();
        logger.LogInformation("Health Check enabled at: '/health'");
    }

    /// <summary>
    /// Builds health check options filtered by tag, writing the response as JSON.
    /// </summary>
    /// <param name="app">The application instance.</param>
    /// <param name="tag">The tag used to filter the health checks.</param>
    /// <returns>The configured <see cref="HealthCheckOptions"/>.</returns>
    private static HealthCheckOptions WriteHealthCheckResponse(this WebApplication app, string tag)
    {
        var options = new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains(tag),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = async (context, report) =>
            {
                var result = new
                {
                    status = report.Status.ToString(),
                    healthChecks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        errorMessage = e.Value.Exception?.Message,
                        hostEnvironment = app.Environment.EnvironmentName.ToLowerInvariant()
                    }),
                };
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsJsonAsync(result);
            },
        };

        return options;
    }
}
