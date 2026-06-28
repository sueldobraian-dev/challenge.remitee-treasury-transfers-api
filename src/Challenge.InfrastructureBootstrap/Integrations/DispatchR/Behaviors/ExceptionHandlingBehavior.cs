using System.Text.Json;
using Challenge.Application.Common.DispatchR;
using Challenge.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Challenge.InfrastructureBootstrap.Integrations.DispatchR.Behaviors;

public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;

    public ExceptionHandlingBehavior(IHttpContextAccessor httpContextAccessor, ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException domainEx)
        {
            _logger.LogWarning(domainEx, "Domain exception caught in DispatchR behavior: {ErrorCode} - {Message}", domainEx.Code, domainEx.Message);
            await WriteErrorResponseAsync((int)domainEx.StatusCode, domainEx.Code, domainEx.Message);
            return default!;
        }
        catch (DbUpdateConcurrencyException concurrencyEx)
        {
            _logger.LogError(concurrencyEx, "A database concurrency conflict occurred in DispatchR behavior.");
            await WriteErrorResponseAsync(StatusCodes.Status409Conflict, "CONCURRENCY_CONFLICT", "A concurrency conflict occurred. Please retry your request.");
            return default!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request execution in DispatchR behavior.");
            await WriteErrorResponseAsync(StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR", "An unexpected error occurred. Please try again later.");
            return default!;
        }
    }

    private async Task WriteErrorResponseAsync(int statusCode, string errorCode, string message)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            code = errorCode,
            message = message
        });

        await context.Response.WriteAsync(payload);
    }
}
