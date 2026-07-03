using Challenge.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Challenge.Infrastructure.Services;

public class KafkaConnectionException : Exception
{
    public KafkaConnectionException(string message) : base(message) { }
    public KafkaConnectionException(string message, Exception innerException) : base(message, innerException) { }
}

public class KafkaService : IKafkaService
{
    private readonly ILogger<KafkaService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private int _simulatedFailureCount = 0;

    public KafkaService(ILogger<KafkaService> logger)
    {
        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<KafkaConnectionException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(50),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Transient Kafka connection issue. Retry attempt {AttemptNumber}. Retrying in {RetryDelayMs}ms.",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            .Build();
    }

    public void SetSimulatedFailures(int count)
    {
        _simulatedFailureCount = count;
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken) where T : class
    {
        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            if (_simulatedFailureCount > 0)
            {
                _simulatedFailureCount--;
                _logger.LogWarning("Simulating Kafka connection failure. Remaining failures to simulate: {Remaining}", _simulatedFailureCount);
                throw new KafkaConnectionException("Failed to connect to Kafka broker (simulated).");
            }

            _logger.LogInformation("Successfully published message of type {MessageType} to topic '{Topic}'.", typeof(T).Name, topic);
            await Task.CompletedTask;
        }, cancellationToken);
    }
}
