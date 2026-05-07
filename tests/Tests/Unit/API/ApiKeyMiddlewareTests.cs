using API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Tests.Unit.API;

/// <summary>
/// Unit tests per ApiKeyMiddleware — autenticazione API Key (BR-API-001).
/// </summary>
public class ApiKeyMiddlewareTests
{
    private static IConfiguration CreateConfig(params string[] keys)
    {
        var dict = new Dictionary<string, string?>();
        for (int i = 0; i < keys.Length; i++)
        {
            dict[$"ApiKeys:Key{i}"] = keys[i];
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    private static ApiKeyMiddleware CreateMiddleware(
        RequestDelegate next, params string[] keys)
    {
        IConfiguration config = CreateConfig(keys);
        return new ApiKeyMiddleware(next, config);
    }

    [Fact]
    public async Task MissingApiKey_Returns401()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "VALID-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/devices";

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task InvalidApiKey_Returns401()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "VALID-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/devices";
        context.Request.Headers["X-Api-Key"] = "WRONG-KEY";

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task ValidApiKey_CallsNext()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "VALID-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/devices";
        context.Request.Headers["X-Api-Key"] = "VALID-KEY";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task SwaggerPath_BypassesAuthentication()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "VALID-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task OpenApiPath_BypassesAuthentication()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "VALID-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/openapi/v1.json";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task EmptyApiKey_Returns401()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "VALID-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/dictionaries";
        context.Request.Headers["X-Api-Key"] = "";

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task MultipleValidKeys_AcceptsAny()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "KEY-ONE", "KEY-TWO");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/commands";
        context.Request.Headers["X-Api-Key"] = "KEY-TWO";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task NoKeysConfigured_AllRequestsRejected()
    {
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/devices";
        context.Request.Headers["X-Api-Key"] = "ANY-KEY";

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
    }
}
