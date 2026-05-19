using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Enums;
using Core.Enums.Auth;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Tests.Integration.API.Auth;

/// <summary>
/// HTTP-level integration tests for <c>POST /api/admin/bootstrap-tokens</c>
/// per <c>specs/001-bootstrap-registration/contracts/admin-bootstrap-tokens.md</c>.
/// Covers T051–T055.
/// </summary>
public class AdminBootstrapTokenEndpointTests : IDisposable
{
    private static readonly Regex PlaintextPattern = new(@"^stbt_[A-Za-z0-9_-]{43}$",
        RegexOptions.Compiled);

    private readonly AdminAuthApiFactory _factory = new();

    private HttpClient AdminClient()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", AdminAuthApiFactory.AdminKey);
        return client;
    }

    private static StringContent JsonBody(object payload)
        => new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    [Fact]
    public async Task Mint_WithDefaultTtl_Returns201AndPlaintextOnce()
    {
        // T051: default TTL of 30 days, plaintext returned, exactly one
        // BootstrapTokens row in Status=Issued.
        using HttpClient client = AdminClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester" }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument body = await ReadJsonAsync(response);
        int tokenId = body.RootElement.GetProperty("tokenId").GetInt32();
        string clientApp = body.RootElement.GetProperty("clientApp").GetString()!;
        string plaintext = body.RootElement.GetProperty("plaintext").GetString()!;
        DateTime mintedAt = body.RootElement.GetProperty("mintedAt").GetDateTime().ToUniversalTime();
        DateTime expiresAt = body.RootElement.GetProperty("expiresAt").GetDateTime().ToUniversalTime();

        Assert.True(tokenId > 0);
        Assert.Equal("ButtonPanelTester", clientApp);
        Assert.Matches(PlaintextPattern, plaintext);
        Assert.Equal(AdminAuthApiFactory.FixedNow, mintedAt);
        Assert.Equal(AdminAuthApiFactory.FixedNow + TimeSpan.FromDays(30), expiresAt);

        await using AppDbContext db = _factory.NewContext();
        BootstrapTokenEntity[] rows = await db.BootstrapTokens.AsNoTracking().ToArrayAsync();
        Assert.Single(rows);
        Assert.Equal(BootstrapTokenStatus.Issued, rows[0].Status);
        Assert.Equal(tokenId, rows[0].Id);
        // Plaintext must NOT be persisted — only its hash.
        Assert.NotEqual(plaintext, rows[0].SecretHash);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2160)]
    public async Task Mint_WithExplicitTtl_HonorsBounds_Accepts(int ttlHours)
    {
        // T052 — accept inclusive bounds.
        using HttpClient client = AdminClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester", ttlHours }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument body = await ReadJsonAsync(response);
        DateTime mintedAt = body.RootElement.GetProperty("mintedAt").GetDateTime().ToUniversalTime();
        DateTime expiresAt = body.RootElement.GetProperty("expiresAt").GetDateTime().ToUniversalTime();
        TimeSpan ttl = expiresAt - mintedAt;
        Assert.InRange(ttl, TimeSpan.FromHours(ttlHours) - TimeSpan.FromSeconds(2),
            TimeSpan.FromHours(ttlHours) + TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2161)]
    [InlineData(-5)]
    public async Task Mint_WithExplicitTtl_HonorsBounds_Rejects(int ttlHours)
    {
        // T052 — reject out-of-bounds with the contract body verbatim.
        using HttpClient client = AdminClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester", ttlHours }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument body = await ReadJsonAsync(response);
        Assert.Equal("ttlHours out of range [1, 2160]",
            body.RootElement.GetProperty("error").GetString());

        await using AppDbContext db = _factory.NewContext();
        Assert.Equal(0, await db.BootstrapTokens.CountAsync());
    }

    [Fact]
    public async Task Mint_PlaintextNotRetrievable()
    {
        // T053 — guards FR-014: no admin endpoint currently exposes the plaintext
        // after the initial 201 response. The full surface check (including the
        // GET /api/admin/installations list response) lands in T067 with US3;
        // this test guards against a regression that would expose the plaintext
        // via any *currently mapped* admin endpoint.
        using HttpClient client = AdminClient();
        HttpResponseMessage mint = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester" }));
        Assert.Equal(HttpStatusCode.Created, mint.StatusCode);

        using JsonDocument body = await ReadJsonAsync(mint);
        string plaintext = body.RootElement.GetProperty("plaintext").GetString()!;

        // Repeat the same POST with the same clientApp — the response *will*
        // contain a fresh plaintext for the new row, but it must not echo the
        // first plaintext back. (Stronger surface checks land with US3.)
        HttpResponseMessage second = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester" }));
        string secondBody = await second.Content.ReadAsStringAsync();
        Assert.DoesNotContain(plaintext, secondBody);

        // And the row in the DB stores only a hash.
        await using AppDbContext db = _factory.NewContext();
        BootstrapTokenEntity[] rows = await db.BootstrapTokens.AsNoTracking().ToArrayAsync();
        Assert.All(rows, r => Assert.NotEqual(plaintext, r.SecretHash));
    }

    [Fact]
    public async Task Mint_WithoutAdminKey_Returns401_NoHeader()
    {
        // T054 — no X-Api-Key header at all.
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester" }));

        await AssertContractUnauthorizedAsync(response);
    }

    [Fact]
    public async Task Mint_WithoutAdminKey_Returns401_KeyInApiKeysButNotAdminKeys()
    {
        // T054 — key authenticates /api/dictionaries but is NOT in AdminApiKeys.
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", AdminAuthApiFactory.NonAdminKey);

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester" }));

        await AssertContractUnauthorizedAsync(response);
    }

    [Fact]
    public async Task Mint_WithoutAdminKey_Returns401_UnknownKey()
    {
        // T054 — key not in either section.
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "not-a-real-key");

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester" }));

        await AssertContractUnauthorizedAsync(response);
    }

    private static async Task AssertContractUnauthorizedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("API key missing or invalid.",
            body.RootElement.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Mint_EmptyClientApp_Returns400(string clientApp)
    {
        // T055 first half — empty / whitespace clientApp.
        using HttpClient client = AdminClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument body = await ReadJsonAsync(response);
        Assert.Equal("clientApp is required",
            body.RootElement.GetProperty("error").GetString());

        await using AppDbContext db = _factory.NewContext();
        Assert.Equal(0, await db.BootstrapTokens.CountAsync());
    }

    [Fact]
    public async Task Mint_MissingClientAppField_Returns400()
    {
        // T055 — body without the clientApp field.
        using HttpClient client = AdminClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { ttlHours = 24 }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument body = await ReadJsonAsync(response);
        Assert.Equal("clientApp is required",
            body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Mint_AuditEntryWritten()
    {
        // T055 second half — IAuditService.LogCreateAsync writes a row with
        // EntityType=BootstrapToken, EntityId=<new id>, ChangedById=<system-admin>.
        using HttpClient client = AdminClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/admin/bootstrap-tokens",
            JsonBody(new { clientApp = "ButtonPanelTester" }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument body = await ReadJsonAsync(response);
        int tokenId = body.RootElement.GetProperty("tokenId").GetInt32();

        await using AppDbContext db = _factory.NewContext();
        UserEntity admin = await db.Users.SingleAsync(u => u.Username == "system-admin");
        AuditEntryEntity[] entries = await db.AuditEntries.AsNoTracking()
            .Where(a => a.EntityType == AuditEntityType.BootstrapToken && a.EntityId == tokenId)
            .ToArrayAsync();

        Assert.Single(entries);
        Assert.Equal(admin.Id, entries[0].ChangedById);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.NotNull(entries[0].NewValue);
        // The audit payload must NOT contain the plaintext.
        string plaintext = body.RootElement.GetProperty("plaintext").GetString()!;
        Assert.DoesNotContain(plaintext, entries[0].NewValue!);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
