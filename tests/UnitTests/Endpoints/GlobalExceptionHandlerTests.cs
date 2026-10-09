using System.Diagnostics;
using System.Text.Json;
using Domain.Exceptions.Desks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Web.Api.Infrastructure;

namespace UnitTests.Endpoints;

public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task UnexpectedException_ExposesDetailsOnlyInDevelopment(string environmentName)
    {
        using var services = CreateServices();
        await using var body = new MemoryStream();
        var context = CreateContext(services, body);
        var handler = CreateHandler(services, environmentName);
        var exception = new InvalidOperationException("Secret database connection", new Exception("Secret inner error"));

        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        var problem = json.RootElement;

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Internal Server Error", problem.GetProperty("title").GetString());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", problem.GetProperty("type").GetString());
        if (string.Equals(environmentName, Environments.Development, StringComparison.Ordinal))
        {
            Assert.Equal(exception.Message, problem.GetProperty("detail").GetString());
            Assert.DoesNotContain("Secret inner error", problem.GetRawText(), StringComparison.Ordinal);
            Assert.DoesNotContain(nameof(InvalidOperationException), problem.GetRawText(), StringComparison.Ordinal);
        }
        else
        {
            Assert.False(problem.TryGetProperty("detail", out _));
            Assert.DoesNotContain("Secret", problem.GetRawText(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task DomainException_ReturnsSafeMessageAndRequestMetadataInProduction()
    {
        using var activity = new Activity("ProblemDetailsTest").Start();
        using var services = CreateServices();
        await using var body = new MemoryStream();
        var context = CreateContext(services, body);
        var handler = CreateHandler(services, Environments.Production);
        var exception = new DeskNotFoundException(Guid.NewGuid());
        var before = DateTime.UtcNow;

        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        var problem = json.RootElement;

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(StatusCodes.Status404NotFound, problem.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.Equal(exception.Message, problem.GetProperty("detail").GetString());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", problem.GetProperty("type").GetString());
        Assert.Equal("GET /api/desks/desk-id", problem.GetProperty("instance").GetString());
        Assert.Equal(context.TraceIdentifier, problem.GetProperty("traceId").GetString());
        var timestamp = problem.GetProperty("timestamp").GetDateTime();
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        Assert.InRange(timestamp, before, DateTime.UtcNow);
        Assert.DoesNotContain("private-query-value", problem.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationException_ReturnsBadRequestWithErrorsByField()
    {
        using var services = CreateServices();
        await using var body = new MemoryStream();
        var context = CreateContext(services, body);
        var handler = CreateHandler(services, Environments.Production);
        var exception = new ValidationException(
        [
            new ValidationFailure("Name", "Name is required."),
            new ValidationFailure("Name", "Name is too short."),
            new ValidationFailure("Page", "Page must be positive.")
        ]);

        await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        var problem = json.RootElement;

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.GetProperty("status").GetInt32());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problem.GetProperty("type").GetString());
        Assert.Equal("One or more validation errors occurred.", problem.GetProperty("title").GetString());
        Assert.Equal(2, problem.GetProperty("errors").GetProperty("Name").GetArrayLength());
        Assert.Equal("Page must be positive.", problem.GetProperty("errors").GetProperty("Page")[0].GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task BadHttpRequestException_PreservesStatusWithoutExposingMessageInProduction()
    {
        using var services = CreateServices();
        await using var body = new MemoryStream();
        var context = CreateContext(services, body);
        var handler = CreateHandler(services, Environments.Production);

        await handler.TryHandleAsync(context, new BadHttpRequestException("Secret binding details"),
            TestContext.Current.CancellationToken);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Bad Request", json.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain("Secret", json.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnsupportedAcceptHeader_ReturnsFalseWhenProblemCannotBeWritten()
    {
        using var services = CreateServices();
        await using var body = new MemoryStream();
        var context = CreateContext(services, body);
        context.Request.Headers.Accept = "text/plain";
        var handler = CreateHandler(services, Environments.Production);

        var handled = await handler.TryHandleAsync(context, new DeskNotFoundException(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(0, body.Length);
    }

    private static ServiceProvider CreateServices() => new ServiceCollection().AddLogging()
        .AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsConfiguration.Customize)
        .BuildServiceProvider();

    private static GlobalExceptionHandler CreateHandler(IServiceProvider services, string environmentName)
    {
        var environment = Mock.Of<IHostEnvironment>(host => host.EnvironmentName == environmentName);
        return new GlobalExceptionHandler(services.GetRequiredService<IProblemDetailsService>(), environment,
            NullLogger<GlobalExceptionHandler>.Instance);
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services, Stream body)
    {
        var context = new DefaultHttpContext { RequestServices = services, TraceIdentifier = "test-trace-id" };
        context.Request.Method = "GET";
        context.Request.PathBase = "/api";
        context.Request.Path = "/desks/desk-id";
        context.Request.QueryString = new QueryString("?token=private-query-value");
        context.Response.Body = body;
        return context;
    }
}
