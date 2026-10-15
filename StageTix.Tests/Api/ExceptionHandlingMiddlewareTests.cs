using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StageTix.API.Middleware;
using StageTix.Core.DTOs.Common;
using StageTix.Core.Exceptions;
using StageTix.Tests.TestHelpers;

namespace StageTix.Tests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private readonly ListLogger<ExceptionHandlingMiddleware> _logger = new();

    private async Task<(int Status, ErrorResponse Body)> RunWithAsync(Exception exception)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "corr-123" };
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, _logger);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ErrorResponse>(context.Response.Body, JsonSerializerOptions.Web);
        return (context.Response.StatusCode, body!);
    }

    public static TheoryData<Exception, int, LogLevel> KnownErrors => new()
    {
        { new BusinessRuleException("bad"), 400, LogLevel.Information },
        { new AuthenticationFailedException(), 401, LogLevel.Information },
        { new NotFoundException("Show", 5), 404, LogLevel.Information },
        { new ConflictException("taken"), 409, LogLevel.Warning },
        { new ConcurrencyConflictException("race"), 409, LogLevel.Warning }
    };

    [Theory]
    [MemberData(nameof(KnownErrors))]
    public async Task Each_handled_error_gets_its_status_code_the_uniform_body_and_the_right_log_level(
        Exception exception, int expectedStatus, LogLevel expectedLevel)
    {
        var (status, body) = await RunWithAsync(exception);

        Assert.Equal(expectedStatus, status);
        Assert.Equal(expectedStatus, body.Status);
        Assert.Equal(exception.Message, body.Detail);
        Assert.Equal("corr-123", body.CorrelationId);
        Assert.Contains(_logger.Entries, e => e.Level == expectedLevel);
        Assert.DoesNotContain(_logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task An_unexpected_exception_is_logged_as_Error_and_returns_500_without_internal_details()
    {
        var (status, body) = await RunWithAsync(new InvalidOperationException("SELECT * FROM secret_table failed"));

        Assert.Equal(500, status);
        Assert.DoesNotContain("secret_table", body.Detail);
        var error = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Error);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }
}
