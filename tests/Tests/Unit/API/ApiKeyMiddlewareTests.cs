using API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Services.Interfaces.Auth;

namespace Tests.Unit.API;

/// <summary>
/// Unit tests for ApiKeyMiddleware — API Key authentication (BR-API-001)
/// plus spec 001 union mode (FR-005) and the /register allow-list entry.
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

    private sealed class StubValidator : IInstallationCredentialValidator
    {
        private readonly Dictionary<string, int> _hits;

        public StubValidator(Dictionary<string, int>? hits = null) => _hits = hits ?? [];

        public Task<int?> ValidateAsync(string plaintext, CancellationToken ct = default)
            => Task.FromResult(_hits.TryGetValue(plaintext, out int id) ? id : (int?)null);

        public void Invalidate(string plaintext) => _hits.Remove(plaintext);
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

        await middleware.InvokeAsync(context, new StubValidator());

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

        await middleware.InvokeAsync(context, new StubValidator());

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

        await middleware.InvokeAsync(context, new StubValidator());

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

        await middleware.InvokeAsync(context, new StubValidator());

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

        await middleware.InvokeAsync(context, new StubValidator());

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

        await middleware.InvokeAsync(context, new StubValidator());

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

        await middleware.InvokeAsync(context, new StubValidator());

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

        await middleware.InvokeAsync(context, new StubValidator());

        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task RegisterPath_BypassesAuthentication()
    {
        // Spec 001: /register is the entry point that establishes
        // authentication for a new installation, so it must be reachable
        // without an X-Api-Key header.
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "VALID-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/register";

        await middleware.InvokeAsync(context, new StubValidator());

        Assert.True(called);
    }

    [Fact]
    public async Task UnionMode_KeyNotInApiKeysButMatchesDbCredential_Authenticates()
    {
        // Spec 001 FR-005: an X-Api-Key value that doesn't match any
        // legacy ApiKeys entry but does match an active per-installation
        // credential (validated by IInstallationCredentialValidator) MUST
        // authenticate.
        bool called = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "LEGACY-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/devices";
        context.Request.Headers["X-Api-Key"] = "stak_db-issued-credential";

        var validator = new StubValidator(new Dictionary<string, int>
        {
            ["stak_db-issued-credential"] = 42
        });

        await middleware.InvokeAsync(context, validator);

        Assert.True(called);
    }

    [Fact]
    public async Task UnionMode_LegacyKeyStillAuthenticates_ValidatorNeverConsulted()
    {
        // Existing legacy-key behaviour must be preserved bit-for-bit:
        // the validator is only consulted on a legacy-mismatch.
        bool called = false;
        bool validatorCalled = false;
        ApiKeyMiddleware middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; },
            "LEGACY-KEY");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/devices";
        context.Request.Headers["X-Api-Key"] = "LEGACY-KEY";

        var validator = new TrackingStubValidator(() => validatorCalled = true);

        await middleware.InvokeAsync(context, validator);

        Assert.True(called);
        Assert.False(validatorCalled);
    }

    private sealed class TrackingStubValidator : IInstallationCredentialValidator
    {
        private readonly Action _onCall;
        public TrackingStubValidator(Action onCall) => _onCall = onCall;
        public Task<int?> ValidateAsync(string plaintext, CancellationToken ct = default)
        {
            _onCall();
            return Task.FromResult<int?>(null);
        }
        public void Invalidate(string plaintext) { }
    }
}
