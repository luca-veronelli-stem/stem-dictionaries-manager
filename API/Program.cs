using System.Text.Json;
using System.Text.Json.Serialization;
using API.Endpoints;
using API.Middleware;
using Infrastructure;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Database — stessa configurazione dual provider della GUI
var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
var connString = provider == "Sqlite"
    ? builder.Configuration.GetConnectionString("Sqlite")!
    : builder.Configuration.GetConnectionString("SqlServer")
      ?? Environment.GetEnvironmentVariable("STEM_DICTIONARIES_CONN_STRING")
      ?? throw new InvalidOperationException(
          "Connection string non trovata. Configura 'ConnectionStrings:SqlServer' o env var 'STEM_DICTIONARIES_CONN_STRING'.");

builder.Services.AddInfrastructure(connString, useSqlServer: provider != "Sqlite");
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

app.UseHttpsRedirection();

// Autenticazione API Key (BR-API-001)
app.UseMiddleware<ApiKeyMiddleware>();

// Endpoint
app.MapDeviceEndpoints();
app.MapDictionaryEndpoints();
app.MapCommandEndpoints();
app.MapBoardEndpoints();

app.Run();
