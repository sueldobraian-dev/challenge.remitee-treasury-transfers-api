using System;
using System.Threading;
using System.Threading.Tasks;
using Challenge.Infrastructure.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Challenge.UnitTests;

public class KafkaServiceTests
{
    private readonly ILogger<KafkaService> _logger;

    public KafkaServiceTests()
    {
        _logger = A.Fake<ILogger<KafkaService>>();
    }

    [Fact]
    public async Task PublishAsync_WhenNoFailures_ShouldSucceed()
    {
        // Arrange
        var service = new KafkaService(_logger);
        var message = new { Data = "test" };

        // Act
        Func<Task> act = async () => await service.PublishAsync("test-topic", message, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenTransientFailureWithinMaxRetries_ShouldRetryAndSucceed()
    {
        // Arrange
        var service = new KafkaService(_logger);
        service.SetSimulatedFailures(2); // Retries are max 3, so 2 failures should be retried and eventually succeed on 3rd attempt
        var message = new { Data = "test" };

        // Act
        Func<Task> act = async () => await service.PublishAsync("test-topic", message, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenTransientFailuresExceedMaxRetries_ShouldExhaustRetriesAndThrow()
    {
        // Arrange
        var service = new KafkaService(_logger);
        service.SetSimulatedFailures(4); // Max 3 retries means 4 attempts total. 4 failures will exceed this and throw.
        var message = new { Data = "test" };

        // Act
        Func<Task> act = async () => await service.PublishAsync("test-topic", message, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KafkaConnectionException>()
            .WithMessage("Failed to connect to Kafka broker (simulated).");
    }
}
