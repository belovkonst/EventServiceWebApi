using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventServiceWebApi.Presentation.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var (statusCode, title, type) = MapStatusCode(ex);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(ex, "Unhandled server error during {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Client error {StatusCode} on {Method} {Path}: {ErrorMessage}",
                statusCode, httpContext.Request.Method, httpContext.Request.Path, ex.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var error = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "Internal server error" : ex.Message,
            Instance = httpContext.Request.Path
        };

        await httpContext.Response.WriteAsJsonAsync(error);
    }

    private static (int Status, string Title, string Type) MapStatusCode(Exception ex)
        => ex switch
        {
            ValidationException or ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };
}
