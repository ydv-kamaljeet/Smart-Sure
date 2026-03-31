using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Shared.Common.Exceptions;

namespace SmartSure.Identity.API.Middleware;

/// <summary>
/// Global exception handler for the Identity service.
/// Implements IExceptionHandler (ASP.NET Core 8+) — registered via AddExceptionHandler&lt;T&gt;.
/// Catches all unhandled exceptions thrown anywhere in the request pipeline and converts them
/// into RFC 7807 ProblemDetails JSON responses with appropriate HTTP status codes.
/// This keeps error handling logic out of individual controllers.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Entry point called by the ASP.NET Core exception handling middleware.
    /// Logs the exception, maps it to an HTTP status + ProblemDetails body, and writes the response.
    /// Returns true to signal that the exception has been handled (stops further propagation).
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Always log the full stack trace for diagnostics regardless of exception type
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        // Map each domain exception type to the appropriate HTTP status and problem title.
        // The wildcard arm (_) handles any unexpected/unclassified exceptions as 500.
        var problemDetails = exception switch
        {
            NotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            // ValidationException carries an Errors dictionary for field-level detail
            Shared.Common.Exceptions.ValidationException ex => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation Error",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807",
                Extensions = { ["errors"] = ex.Errors }
            },
            UnauthorizedException ex => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            BadRequestException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            ConflictException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            // Catch-all: hide internal details from the client in case of unexpected errors
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7807"
            }
        };

        // Write the status code and ProblemDetails JSON back to the HTTP response
        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true; // exception is fully handled — no further middleware processing required
    }
}
