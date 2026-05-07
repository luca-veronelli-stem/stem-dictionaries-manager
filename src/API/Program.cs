using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Endpoints;
using API.Middleware;
using Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database — logica di risoluzione centralizzata in Infrastructure
string provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
bool useSqlServer = !provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
string connString = Infrastructure.DependencyInjection.ResolveConnectionString(
    builder.Configuration.GetConnectionString(useSqlServer ? "SqlServer" : "Sqlite"),
    useSqlServer);

builder.Services.AddInfrastructure(connString, useSqlServer);
builder.Services.AddServices();

// JSON: camelCase + null omessi (BR-API-004)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// Health check con verifica connessione DB
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

WebApplication app = builder.Build();

// Swagger UI solo in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Stem.Dictionaries.Manager API v1"));
}

// Gestione errori DB — 503 Service Unavailable con JSON strutturato (API-004)
app.UseMiddleware<DatabaseExceptionMiddleware>();

// Autenticazione API Key (BR-API-001)
app.UseMiddleware<ApiKeyMiddleware>();

// Endpoint
app.MapDeviceEndpoints();
app.MapDictionaryEndpoints();
app.MapCommandEndpoints();
app.MapBoardEndpoints();

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
