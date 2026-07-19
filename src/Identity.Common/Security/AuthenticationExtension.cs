using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Identity.Common.Security;

/// <summary>
/// Extension methods for registering JWT authentication.
/// </summary>
/// <remarks>
/// Validation currently relies on a symmetric key (HS256) read from <c>Jwt:SecretKey</c>.
/// When migrating to RS256/JWKS only this extension has to change: the public key
/// gets resolved through <c>Authority</c>/JWKS and <c>ValidateIssuer</c>/
/// <c>ValidateAudience</c> get enabled.
/// </remarks>
public static class AuthenticationExtension
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        var secretKey = configuration["Jwt:SecretKey"];
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        var key = Encoding.ASCII.GetBytes(secretKey);

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }
}
