using FluentValidation;
using Identity.Common.Validation;
using Identity.Domain.Exceptions;
using Identity.WebApi.Common;
using System.Data.Common;
using System.Net;
using System.Text.Json;

namespace Identity.WebApi.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them into standardized API responses.
/// Exception-specific mappings are added along with the use cases.
/// </summary>
public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException)
        {
            // Delegated to ValidationExceptionMiddleware, which maps it to a 400 response.
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedExceptionAsync(context, ex);
        }
        catch (ConflictException ex)
        {
            await HandleConflictExceptionAsync(context, ex);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            await HandleDatabaseFailureAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Tells a persistence failure apart from an application error by looking for a
    /// <see cref="DbException"/> anywhere in the exception chain.
    /// </summary>
    /// <remarks>
    /// The check walks the chain because the provider rarely surfaces its own type: an
    /// unreachable PostgreSQL arrives as <c>InvalidOperationException</c> ("An exception has
    /// been raised that is likely due to a transient failure") wrapping an
    /// <c>NpgsqlException</c>, itself wrapping a <c>SocketException</c>. Matching on the
    /// outer type alone is what used to report a database outage as "409 Conflict".
    /// </remarks>
    private static bool IsDatabaseFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException)
            {
                return true;
            }
        }

        return false;
    }

    private async Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
    {
        _logger.LogInformation("Unauthorized request: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        var response = new ApiResponse
        {
            Success = false,
            Message = string.IsNullOrWhiteSpace(exception.Message) ? "Unauthorized" : exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private async Task HandleConflictExceptionAsync(HttpContext context, ConflictException exception)
    {
        _logger.LogWarning(exception, "Conflict occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Conflict;

        var response = new ApiResponse
        {
            Success = false,
            Message = string.IsNullOrWhiteSpace(exception.Message) ? "Operation conflict" : exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    /// <summary>
    /// Answers a persistence failure with 503, so a client can tell "your request clashed
    /// with existing data" (409) from "the service cannot reach its database right now".
    /// The message stays generic: connection strings and host names never reach the client.
    /// </summary>
    private async Task HandleDatabaseFailureAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Database unavailable: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;

        var response = new ApiResponse
        {
            Success = false,
            Message = "The service is temporarily unavailable. Please try again later."
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private async Task HandleGenericExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unexpected error occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ApiResponse
        {
            Success = false,
            Message = "An unexpected error occurred. Please try again later.",
#if DEBUG
            Errors =
            [
                new ValidationErrorDetail
                {
                    Error = "Exception",
                    Detail = exception.ToString()
                }
            ]
#endif
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
