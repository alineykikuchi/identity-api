using Identity.Application;
using Identity.Common.HealthChecks;
using Identity.Common.Logging;
using Identity.Common.Security;
using Identity.Common.Validation;
using Identity.IoC;
using Identity.ORM;
using Identity.WebApi.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Identity.WebApi;

public class Program
{
    // Stays void, and the failure exit code goes through Environment.ExitCode: the
    // functional tests reach this entry point through WebApplicationFactory, whose host
    // resolution only recognizes the void signature. An int return makes every test in
    // that project fail with "The entry point exited without ever building an IHost".
    public static void Main(string[] args)
    {
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.AddDefaultLogging();

            // Logged after AddDefaultLogging, not before: until the bootstrap logger is
            // installed the static Log has nowhere to write.
            Log.Information("Starting web application");

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.AddBasicHealthChecks();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Identity.ORM")
                )
            );

            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.RegisterDependencies();

            builder.Services.AddAutoMapper(typeof(Program).Assembly, typeof(ApplicationLayer).Assembly);

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(ApplicationLayer).Assembly,
                    typeof(Program).Assembly
                );
            });

            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            var app = builder.Build();
            app.UseMiddleware<ValidationExceptionMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseBasicHealthChecks();

            app.MapControllers();

            app.Run();
        }
        // HostAbortedException is let through on purpose. The EF Core design-time tools
        // (dotnet ef migrations/database) and WebApplicationFactory both build the host and
        // abort it by design to get at the service provider; treating that as a crash would
        // bury every migration command in a stack trace.
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            // Written to stderr as well as Serilog: this catch has to stay useful even when
            // the failure happened before logging was configured -- a startup that dies here
            // silently is what makes a busy port look like the app doing nothing at all.
            Console.Error.WriteLine($"Application terminated unexpectedly: {ex}");
            Log.Fatal(ex, "Application terminated unexpectedly");

            // Set rather than returned: Main has to keep its void signature (see above).
            Environment.ExitCode = 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
