using Challenge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Challenge.InfrastructureBootstrap.Integrations.Persistence;

public class RetryUnitOfWorkDecorator : IUnitOfWork
{
    private readonly IUnitOfWork _inner;
    private readonly ILogger<RetryUnitOfWorkDecorator> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public RetryUnitOfWorkDecorator(IUnitOfWork inner, ILogger<RetryUnitOfWorkDecorator> logger)
    {
        _inner = inner;
        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<DbUpdateConcurrencyException>()
                    .Handle<DbUpdateException>(ex =>
                        ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                        (sqlEx.Number == 1205 || sqlEx.Number == -2 || sqlEx.Number == 53 || sqlEx.Number == 64))
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(100),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Transient exception caught on SaveChangesAsync attempt {AttemptNumber}. Retrying in {RetryDelayMs}ms.",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            .Build();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _resiliencePipeline.ExecuteAsync(
            async state => await _inner.SaveChangesAsync(state),
            cancellationToken);
    }
}
