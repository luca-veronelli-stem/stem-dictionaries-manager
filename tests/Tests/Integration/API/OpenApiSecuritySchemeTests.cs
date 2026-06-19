using System.Linq;
using System.Text.Json;

namespace Tests.Integration.API;

/// <summary>
/// Verifies the #12 fix: the generated OpenAPI document declares the
/// <c>X-Api-Key</c> security scheme and applies it as a document-wide
/// requirement, so the Swagger UI renders an "Authorize" button and sends the
/// header on try-it-out calls. Before the fix the Microsoft.OpenApi v2 document
/// carried no security scheme at all.
/// </summary>
public class OpenApiSecuritySchemeTests
{
    [Fact]
    public async Task OpenApiDocument_DeclaresApiKeySecurityScheme_AndRequiresIt()
    {
        using var factory = new DevHostApiFactory();
        using HttpClient client = factory.CreateClient();

        string json = await client.GetStringAsync("/openapi/v1.json");

        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        JsonElement scheme = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("ApiKey");
        Assert.Equal("apiKey", scheme.GetProperty("type").GetString());
        Assert.Equal("header", scheme.GetProperty("in").GetString());
        Assert.Equal("X-Api-Key", scheme.GetProperty("name").GetString());

        JsonElement security = root.GetProperty("security");
        Assert.Equal(JsonValueKind.Array, security.ValueKind);
        bool requiresApiKey = security.EnumerateArray()
            .Any(requirement => requirement.TryGetProperty("ApiKey", out _));
        Assert.True(requiresApiKey,
            "document-level security must reference the ApiKey scheme");
    }
}
