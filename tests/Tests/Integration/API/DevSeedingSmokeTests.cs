using System.Net;
using System.Text.Json;

namespace Tests.Integration.API;

/// <summary>
/// Verifies the #87 fix: a fresh <c>dotnet run --project src/API</c> against
/// SQLite serves seeded data without first launching the WPF GUI. Before the
/// fix the API never created or seeded the schema, so the first DB-touching
/// call threw against an empty database; after it, the Development-gated
/// startup block in <c>Program.cs</c> builds and seeds the schema.
/// </summary>
public class DevSeedingSmokeTests
{
    [Fact]
    public async Task FreshSqliteDevHost_ResolvesDbTouchingEndpoint_WithSeededData()
    {
        using var factory = new DevHostApiFactory();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", DevHostApiFactory.ApiKey);

        HttpResponseMessage response = await client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0,
            "the Development startup seed must populate devices");
    }
}
