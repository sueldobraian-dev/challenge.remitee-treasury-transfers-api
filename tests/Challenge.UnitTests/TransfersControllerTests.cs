using Challenge.API.Controllers.v1;
using Challenge.Application.Features.Transfers.Commands;
using Challenge.Application.Features.Transfers.Commands.Responses;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR;
using FakeItEasy;
using FluentAssertions;

namespace Challenge.UnitTests;

public class TransfersControllerTests
{
    [Fact]
    public async Task CreateAsync_ShouldSendCommandToDispatcherAndReturnResult()
    {
        // Arrange
        var dispatcher = A.Fake<IDispatcher>();
        var controller = new TransfersController(dispatcher);

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        var expectedResponse = new TransferResultResponse(
            Guid.NewGuid(),
            command.OperationId,
            "SUCCESS",
            "ACC-USD-1",
            "ACC-USD-2",
            100.00m,
            100.00m,
            DateTimeOffset.UtcNow
        );

        A.CallTo(() => dispatcher.SendAsync(command, A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await controller.CreateAsync(command);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        A.CallTo(() => dispatcher.SendAsync(command, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
