using System.Text.Json;
using System.Text.Json.Serialization;
using API.Endpoints;
using API.Middleware;
using Infrastructure;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Database — logica di risoluzione centralizzata in Infrastructure
var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
var useSqlServer = !provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
var connString = Infrastructure.DependencyInjection.ResolveConnectionString(
    provider,
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

var app = builder.Build();

// Swagger UI solo in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Stem.Dictionaries.Manager API v1"));
}

// Autenticazione API Key (BR-API-001)
app.UseMiddleware<ApiKeyMiddleware>();

// Endpoint
app.MapDeviceEndpoints();
app.MapDictionaryEndpoints();
app.MapCommandEndpoints();
app.MapBoardEndpoints();

app.Run();
