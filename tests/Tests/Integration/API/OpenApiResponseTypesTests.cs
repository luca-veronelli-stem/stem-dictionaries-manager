using System.Text.Json;

namespace Tests.Integration.API;

/// <summary>
/// Verifies the #13 fix: endpoints carry explicit <c>.Produces&lt;T&gt;()</c> /
/// <c>.Produces(StatusCodes…)</c> annotations, so the generated OpenAPI
/// document describes both the success payload type and the documented error
/// statuses. Before the fix the minimal-API handlers returned an untyped
/// <c>IResult</c>, so the document listed a bodyless 200 and no error statuses.
/// </summary>
public class OpenApiResponseTypesTests
{
    [Fact]
    public async Task OpenApiDocument_AnnotatesResponseTypesAndStatuses()
    {
        using var factory = new DevHostApiFactory();
        using HttpClient client = factory.CreateClient();

        string json = await client.GetStringAsync("/openapi/v1.json");

        using var doc = JsonDocument.Parse(json);
        JsonElement responses = doc.RootElement
            .GetProperty("paths")
            .GetProperty("/api/dictionaries/standard")
            .GetProperty("get")
            .GetProperty("responses");

        // 200 now carries a typed JSON body (Produces<DictionaryDetailDto>).
        Assert.True(
            responses.GetProperty("200").TryGetProperty("content", out JsonElement content),
            "the 200 response must declare a typed JSON body");
        Assert.True(content.TryGetProperty("application/json", out _));

        // Documented error statuses are present (group-level 401 + resource 404).
        Assert.True(responses.TryGetProperty("401", out _),
            "the 401 response must be documented");
        Assert.True(responses.TryGetProperty("404", out _),
            "the 404 response must be documented");
    }
}
