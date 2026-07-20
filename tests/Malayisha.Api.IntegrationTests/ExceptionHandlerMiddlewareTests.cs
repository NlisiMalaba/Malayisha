using System.Net;
using System.Text.Json;
using Malayisha.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Api.IntegrationTests;

public sealed class ExceptionHandlerMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenDownstreamThrows_ReturnsProblemDetailsWithCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        context.Items[CorrelationIdMiddleware.ItemKey] = "test-correlation-id";

        RequestDelegate next = _ => throw new InvalidOperationException("Test failure");

        var middleware = new ExceptionHandlerMiddleware(
            next,
            NullLogger<ExceptionHandlerMiddleware>.Instance,
            new TestHostEnvironment { EnvironmentName = Environments.Production });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problemDetails);
        Assert.Equal(500, problemDetails.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.True(problemDetails.Extensions.ContainsKey("correlationId"));
        Assert.Equal("test-correlation-id", problemDetails.Extensions["correlationId"]?.ToString());
        Assert.Null(problemDetails.Detail);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Malayisha.Api.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
