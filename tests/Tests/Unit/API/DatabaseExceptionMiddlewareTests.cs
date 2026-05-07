using System.IO;
using System.Text.Json;
using API.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Tests.Unit.API;

/// <summary>
/// Unit tests per DatabaseExceptionMiddleware (API-004).
/// Verifica che errori DB ritornino 503 con JSON strutturato.
/// </summary>
public class DatabaseExceptionMiddlewareTests
{
    private sealed class StubEnvironment(string environmentName)
        : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static DatabaseExceptionMiddleware CreateMiddleware(
        RequestDelegate next, bool isDevelopment = false)
    {
        var env = new StubEnvironment(
            isDevelopment ? Environments.Development : Environments.Production);
        return new DatabaseExceptionMiddleware(next, env);
    }

    [Fact]
    public async Task NoException_CallsNextAndReturns200()
    {
        bool called = false;
        DatabaseExceptionMiddleware middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task TimeoutException_Returns503()
    {
        DatabaseExceptionMiddleware middleware = CreateMiddleware(
            _ => throw new TimeoutException("Connection timed out"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
    }

    [Fact]
    public async Task TimeoutException_ReturnsJsonWithErrorField()
    {
        DatabaseExceptionMiddleware middleware = CreateMiddleware(
            _ => throw new TimeoutException("Connection timed out"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        JsonDocument json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.True(json.RootElement.TryGetProperty("error", out JsonElement errorProp));
        Assert.Contains("Database unavailable", errorProp.GetString());
    }

    [Fact]
    public async Task TimeoutException_Production_NoDetailField()
    {
        DatabaseExceptionMiddleware middleware = CreateMiddleware(
            _ => throw new TimeoutException("Connection timed out"),
            isDevelopment: false);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        JsonDocument json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(json.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task TimeoutException_Development_IncludesDetailField()
    {
        DatabaseExceptionMiddleware middleware = CreateMiddleware(
            _ => throw new TimeoutException("Connection timed out"),
            isDevelopment: true);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        JsonDocument json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.True(json.RootElement.TryGetProperty("detail", out JsonElement detail));
        Assert.Equal("Connection timed out", detail.GetString());
    }

    [Fact]
    public async Task InnerException_TimeoutException_Returns503()
    {
        DatabaseExceptionMiddleware middleware = CreateMiddleware(
            _ => throw new InvalidOperationException("EF Core wrapper",
                new TimeoutException("Inner timeout")));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
    }

    [Fact]
    public async Task NonDbException_Rethrows()
    {
        DatabaseExceptionMiddleware middleware = CreateMiddleware(
            _ => throw new ArgumentException("Not a DB error"));
        var context = new DefaultHttpContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task ContentType_IsApplicationJson()
    {
        DatabaseExceptionMiddleware middleware = CreateMiddleware(
            _ => throw new TimeoutException("timeout"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.StartsWith("application/json", context.Response.ContentType);
    }
}
