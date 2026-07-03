using Challenge.Domain.Repositories;
using Challenge.InfrastructureBootstrap.Integrations.Persistence;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Challenge.UnitTests;

public class RetryUnitOfWorkDecoratorTests
{
    private readonly IUnitOfWork _innerUnitOfWork;
    private readonly ILogger<RetryUnitOfWorkDecorator> _logger;

    public RetryUnitOfWorkDecoratorTests()
    {
        _innerUnitOfWork = A.Fake<IUnitOfWork>();
        _logger = A.Fake<ILogger<RetryUnitOfWorkDecorator>>();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoException_ShouldCallInnerOnceAndReturnCount()
    {
        // Arrange
        var decorator = new RetryUnitOfWorkDecorator(_innerUnitOfWork, _logger);
        A.CallTo(() => _innerUnitOfWork.SaveChangesAsync(A<CancellationToken>._)).Returns(1);

        // Act
        var result = await decorator.SaveChangesAsync(CancellationToken.None);

        // Assert
        result.Should().Be(1);
        A.CallTo(() => _innerUnitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDbUpdateConcurrencyExceptionThrownAndSucceedsOnRetry_ShouldRetryAndReturnCount()
    {
        // Arrange
        var decorator = new RetryUnitOfWorkDecorator(_innerUnitOfWork, _logger);
        var callCount = 0;
        A.CallTo(() => _innerUnitOfWork.SaveChangesAsync(A<CancellationToken>._)).ReturnsLazily(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict");
            }
            return 1;
        });

        // Act
        var result = await decorator.SaveChangesAsync(CancellationToken.None);

        // Assert
        result.Should().Be(1);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTimeoutExceptionThrownAndSucceedsOnRetry_ShouldRetryAndReturnCount()
    {
        // Arrange
        var decorator = new RetryUnitOfWorkDecorator(_innerUnitOfWork, _logger);
        var callCount = 0;
        A.CallTo(() => _innerUnitOfWork.SaveChangesAsync(A<CancellationToken>._)).ReturnsLazily(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new TimeoutException("Timeout occurred");
            }
            return 1;
        });

        // Act
        var result = await decorator.SaveChangesAsync(CancellationToken.None);

        // Assert
        result.Should().Be(1);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenExceptionPersists_ShouldExhaustRetriesAndThrow()
    {
        // Arrange
        var decorator = new RetryUnitOfWorkDecorator(_innerUnitOfWork, _logger);
        A.CallTo(() => _innerUnitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .Throws(() => new DbUpdateConcurrencyException("Concurrency conflict"));

        // Act & Assert
        var act = () => decorator.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        A.CallTo(() => _innerUnitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappened(4, Times.OrMore); // 1 initial + 3 retries
    }
}
