using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Challenge.Domain.Exceptions;

namespace Challenge.InfrastructureBootstrap.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (DomainException domainEx)
        {
            _logger.LogWarning(domainEx, "Domain exception caught: {ErrorCode} - {Message}", domainEx.Code, domainEx.Message);
            await HandleExceptionAsync(context, (int)domainEx.StatusCode, domainEx.Code, domainEx.Message);
        }
        catch (DbUpdateConcurrencyException concurrencyEx)
        {
            _logger.LogError(concurrencyEx, "A database concurrency conflict occurred.");
            await HandleExceptionAsync(context, StatusCodes.Status409Conflict, "CONCURRENCY_CONFLICT", "A concurrency conflict occurred. Please retry your request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request execution.");
            await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR", "An unexpected error occurred. Please try again later.");
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, int statusCode, string errorCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            code = errorCode,
            message = message
        });

        return context.Response.WriteAsync(payload);
    }
}
