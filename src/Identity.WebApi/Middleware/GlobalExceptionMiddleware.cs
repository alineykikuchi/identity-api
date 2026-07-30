using FluentValidation;
using Identity.Common.Validation;
using Identity.WebApi.Common;
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
        catch (InvalidOperationException ex)
        {
            await HandleConflictExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericExceptionAsync(context, ex);
        }
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

    private async Task HandleConflictExceptionAsync(HttpContext context, InvalidOperationException exception)
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
