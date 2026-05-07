using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Auth;
using API.Endpoints;
using API.Endpoints.Auth;
using API.Middleware;
using Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Services;
using Services.Auth;
using Services.Interfaces;
using Services.Interfaces.Auth;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database — connection-string resolution centralized in Infrastructure
string provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
bool useSqlServer = !provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
string connString = Infrastructure.DependencyInjection.ResolveConnectionString(
    builder.Configuration.GetConnectionString(useSqlServer ? "SqlServer" : "Sqlite"),
    useSqlServer);

builder.Services.AddInfrastructure(connString, useSqlServer);

// Spec 001 — bootstrap registration:
// HttpContext-aware ICurrentUserProvider (overrides the default singleton
// from AddServices) so AdminAuthenticationMiddleware can stamp per-request
// audit attribution without racing across concurrent calls.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();

builder.Services.AddServices();

// Spec 001 — auth helpers and validator. The validator is scoped (NOT
// singleton): the cache it relies on is the singleton IMemoryCache, so
// the cached resolutions still amortise across requests; the validator
// instance itself is cheap to recreate per request and avoids the
// scoped-dep-from-singleton anti-pattern.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITokenGenerator, TokenGenerator>();
builder.Services.AddScoped<IInstallationCredentialValidator, InstallationCredentialValidator>();

// JSON: camelCase + nulls omitted (BR-API-004)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// Health check that verifies DB connectivity
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

WebApplication app = builder.Build();

// Swagger UI in Development only
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Stem.Dictionaries.Manager API v1"));
}

// DB error handling — 503 Service Unavailable with structured JSON (API-004)
app.UseMiddleware<DatabaseExceptionMiddleware>();

// API Key authentication (BR-API-001 + spec 001 union mode FR-005)
app.UseMiddleware<ApiKeyMiddleware>();

// Admin gate for /api/admin/* (spec 001 § Audit split)
app.UseMiddleware<AdminAuthenticationMiddleware>();

// Endpoints
app.MapDeviceEndpoints();
app.MapDictionaryEndpoints();
app.MapCommandEndpoints();
app.MapBoardEndpoints();
app.MapRegistrationEndpoints();
app.MapAdminAuthEndpoints();

// Health check — GET /health (no auth)
app.MapHealthChecks("/health");

// Version — GET /api/version (no auth)
app.MapGet("/api/version", () => Results.Ok(new
{
    version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
    environment = app.Environment.EnvironmentName
}));

app.Run();

/// <summary>
/// Test entry point — exposes the implicit Program class so
/// <c>WebApplicationFactory&lt;Program&gt;</c> can host the API in
/// integration tests.
/// </summary>
public partial class Program { }
