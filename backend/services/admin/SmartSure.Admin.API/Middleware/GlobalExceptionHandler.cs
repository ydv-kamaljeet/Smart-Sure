using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Shared.Common.Exceptions;

namespace SmartSure.Admin.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            NotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
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
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7807"
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
