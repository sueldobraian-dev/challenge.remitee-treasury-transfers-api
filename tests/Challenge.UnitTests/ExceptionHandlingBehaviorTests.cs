using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Challenge.Application.Common.DispatchR;
using Challenge.Domain.Exceptions;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR.Behaviors;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Challenge.UnitTests;

public class ExceptionHandlingBehaviorTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DefaultHttpContext _httpContext;
    private readonly ILogger<ExceptionHandlingBehavior<IRequest<object>, object>> _logger;

    public ExceptionHandlingBehaviorTests()
    {
        _httpContextAccessor = A.Fake<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream(); // Use a memory stream to inspect output

        A.CallTo(() => _httpContextAccessor.HttpContext).Returns(_httpContext);
        _logger = A.Fake<ILogger<ExceptionHandlingBehavior<IRequest<object>, object>>>();
    }

    [Fact]
    public async Task HandleAsync_WhenNoException_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<IRequest<object>, object>(_httpContextAccessor, _logger);
        var request = A.Fake<IRequest<object>>();
        var expectedResponse = new object();
        RequestHandlerDelegate<object> next = () => Task.FromResult(expectedResponse);

        // Act
        var response = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        response.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task HandleAsync_WhenDomainExceptionThrown_ShouldWriteErrorResponseAndReturnDefault()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<IRequest<object>, object>(_httpContextAccessor, _logger);
        var request = A.Fake<IRequest<object>>();
        
        var domainException = new TestDomainException("Resource not found", "NOT_FOUND", HttpStatusCode.NotFound);
        RequestHandlerDelegate<object> next = () => throw domainException;

        // Act
        var response = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        response.Should().BeNull();
        _httpContext.Response.StatusCode.Should().Be(404);
        _httpContext.Response.ContentType.Should().Be("application/json");

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var bodyJson = await reader.ReadToEndAsync();

        var payload = JsonDocument.Parse(bodyJson).RootElement;
        payload.GetProperty("code").GetString().Should().Be("NOT_FOUND");
        payload.GetProperty("message").GetString().Should().Be("Resource not found");
    }

    private class TestDomainException : DomainException
    {
        public TestDomainException(string message, string code, HttpStatusCode statusCode)
            : base(message, code, statusCode)
        {
        }
    }
}
