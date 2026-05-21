using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Core.Enums;
using Core.Enums.Auth;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace Tests.Integration.API.Auth;

/// <summary>
/// HTTP-level integration tests for admin per-installation management
/// (T060–T066, US3). Covers <c>GET /api/admin/installations</c> and
/// <c>POST /api/admin/installations/{id}/revoke</c> per
/// <c>specs/001-bootstrap-registration/contracts/admin-installations.md</c>.
/// </summary>
public class AdminInstallationEndpointTests : IDisposable
{
    private readonly AdminAuthApiFactory _factory = new()
    {
        // Register descriptor policies for both clientApps used by the list
        // tests so /register accepts both — production registers only
        // ButtonPanelTester (see Services/DependencyInjection.cs); the wider
        // set here is test-only.
        DescriptorPoliciesOverride = new Dictionary<string, DescriptorPolicy>(StringComparer.Ordinal)
        {
            ["ButtonPanelTester"] = new(OsUserIdRequired: true, MachineIdRequired: true),
            ["GlobalService"] = new(OsUserIdRequired: true, MachineIdRequired: true),
        }
    };
    private readonly PasswordHasher _hasher = new();

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

    /// <summary>
    /// Seeds a bootstrap token directly in the DB so a subsequent
    /// <c>/register</c> can consume it without going through the admin
    /// mint surface.
    /// </summary>
    private async Task SeedTokenAsync(string clientApp, string plaintext, TimeSpan? ttl = null)
    {
        DateTime mintedAt = DateTime.UtcNow.AddSeconds(-30);
        await using AppDbContext db = _factory.NewContext();
        db.BootstrapTokens.Add(new BootstrapTokenEntity
        {
            ClientApp = clientApp,
            SecretHash = _hasher.Hash(plaintext),
            MintedAt = mintedAt,
            ExpiresAt = mintedAt + (ttl ?? TimeSpan.FromDays(30)),
            Status = BootstrapTokenStatus.Issued
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Mints a bootstrap token, calls <c>POST /register</c>, and returns
    /// <c>(installationId, plaintextCredential)</c> so tests can assert on
    /// both DB state and credential round-trips.
    /// </summary>
    private async Task<(int InstallationId, string Plaintext)> RegisterInstallationAsync(
        HttpClient unauthenticated,
        string clientApp,
        string osUserId,
        string machineId,
        Guid installGuid,
        string tokenPlaintext)
    {
        await SeedTokenAsync(clientApp, tokenPlaintext);
        HttpResponseMessage response = await unauthenticated.PostAsync(
            "/register",
            JsonBody(new
            {
                bootstrapToken = tokenPlaintext,
                descriptor = new
                {
                    clientApp,
                    osUserId,
                    machineId,
                    installGuid = installGuid.ToString(),
                    appVersion = "1.0.0"
                }
            }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = await ReadJsonAsync(response);
        return (
            body.RootElement.GetProperty("installationId").GetInt32(),
            body.RootElement.GetProperty("apiCredential").GetString()!);
    }

    [Fact]
    public async Task List_ReturnsAllInstallationsWithMetadata()
    {
        // T060: seed 2 installations across 2 ClientApps; GET returns both
        // with the contract shape and no secretHash/plaintext fields.
        using HttpClient unauthed = _factory.CreateClient();
        using HttpClient admin = AdminClient();

        (int id1, _) = await RegisterInstallationAsync(unauthed,
            clientApp: "ButtonPanelTester",
            osUserId: "S-1-5-21-2127521184-1604012920-1887927527-72713",
            machineId: "8a5e9b3c-6f4d-4d2a-9c1b-7d8e3f4b6c2a",
            installGuid: Guid.Parse("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            tokenPlaintext: "stbt_seed-list-a");
        (int id2, _) = await RegisterInstallationAsync(unauthed,
            clientApp: "GlobalService",
            osUserId: "S-1-5-21-9876543210-1234567890-2468013579-12345",
            machineId: "1b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e",
            installGuid: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef0123456789"),
            tokenPlaintext: "stbt_seed-list-b");

        HttpResponseMessage response = await admin.GetAsync("/api/admin/installations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument body = await ReadJsonAsync(response);
        JsonElement root = body.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());

        // Items must include every metadata field from the contract — and
        // must NOT include any secret material. revokedAt is omitted when
        // null per the global BR-API-004 "nulls omitted" JSON convention;
        // an absent revokedAt is semantically equivalent to null.
        string[] requiredFields =
        [
            "installationId", "clientApp", "osUserId", "machineId",
            "installGuid", "registeredAt", "status"
        ];
        foreach (JsonElement item in root.EnumerateArray())
        {
            foreach (string field in requiredFields)
            {
                Assert.True(item.TryGetProperty(field, out _),
                    $"List item is missing contract field '{field}'.");
            }
            // revokedAt: present-and-null OR absent. Both are valid.
            if (item.TryGetProperty("revokedAt", out JsonElement revokedAt))
            {
                Assert.True(revokedAt.ValueKind is JsonValueKind.Null
                    or JsonValueKind.String);
            }
        }

        // Plaintext / secretHash leak check.
        string raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secretHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apiCredential", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("plaintext", raw, StringComparison.OrdinalIgnoreCase);

        int[] ids = [.. root.EnumerateArray().Select(e => e.GetProperty("installationId").GetInt32())];
        Assert.Contains(id1, ids);
        Assert.Contains(id2, ids);
    }

    [Fact]
    public async Task List_WithFilters_Honored()
    {
        // T061: ?clientApp filter and ?status filter — server-side, not
        // over-fetch + client-side filter.
        using HttpClient unauthed = _factory.CreateClient();
        using HttpClient admin = AdminClient();

        (int btId, _) = await RegisterInstallationAsync(unauthed,
            "ButtonPanelTester",
            "u1", "m1", Guid.Parse("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            "stbt_filter-bt");
        (int gsId, _) = await RegisterInstallationAsync(unauthed,
            "GlobalService",
            "u2", "m2", Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef0123456789"),
            "stbt_filter-gs");

        // Revoke the GlobalService one so we have a mix of active+revoked.
        HttpResponseMessage revoke = await admin.PostAsync(
            $"/api/admin/installations/{gsId}/revoke", content: null);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        // Filter by clientApp.
        using JsonDocument btOnly = await ReadJsonAsync(
            await admin.GetAsync("/api/admin/installations?clientApp=ButtonPanelTester"));
        Assert.Single(btOnly.RootElement.EnumerateArray());
        Assert.Equal(btId, btOnly.RootElement[0].GetProperty("installationId").GetInt32());

        // Filter by status=active.
        using JsonDocument actives = await ReadJsonAsync(
            await admin.GetAsync("/api/admin/installations?status=active"));
        Assert.Single(actives.RootElement.EnumerateArray());
        Assert.Equal(btId, actives.RootElement[0].GetProperty("installationId").GetInt32());

        // Filter by status=revoked.
        using JsonDocument revoked = await ReadJsonAsync(
            await admin.GetAsync("/api/admin/installations?status=revoked"));
        Assert.Single(revoked.RootElement.EnumerateArray());
        Assert.Equal(gsId, revoked.RootElement[0].GetProperty("installationId").GetInt32());

        // status=all (default) returns both.
        using JsonDocument all = await ReadJsonAsync(
            await admin.GetAsync("/api/admin/installations?status=all"));
        Assert.Equal(2, all.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Revoke_TransitionsActiveToRevoked()
    {
        // T062: POST {id}/revoke flips Installation + InstallationApiCredential
        // to Revoked, sets RevokedAt, and returns the contract response body.
        using HttpClient unauthed = _factory.CreateClient();
        using HttpClient admin = AdminClient();

        (int id, _) = await RegisterInstallationAsync(unauthed,
            "ButtonPanelTester", "u", "m",
            Guid.Parse("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            "stbt_revoke-target");

        HttpResponseMessage response = await admin.PostAsync(
            $"/api/admin/installations/{id}/revoke", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument body = await ReadJsonAsync(response);
        Assert.Equal(id, body.RootElement.GetProperty("installationId").GetInt32());
        Assert.Equal("revoked", body.RootElement.GetProperty("status").GetString());
        Assert.False(body.RootElement.GetProperty("revokedAt").ValueKind == JsonValueKind.Null);

        await using AppDbContext db = _factory.NewContext();
        InstallationEntity? install = await db.Installations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
        Assert.NotNull(install);
        Assert.Equal(InstallationStatus.Revoked, install!.Status);
        Assert.NotNull(install.RevokedAt);

        InstallationApiCredentialEntity[] credentials = await db.InstallationApiCredentials
            .AsNoTracking()
            .Where(c => c.InstallationId == id)
            .ToArrayAsync();
        Assert.NotEmpty(credentials);
        Assert.All(credentials, c => Assert.Equal(InstallationStatus.Revoked, c.Status));
        Assert.All(credentials, c => Assert.NotNull(c.RevokedAt));
    }

    [Fact]
    public async Task Revoke_IsolationFromOtherInstallations()
    {
        // T063: revoking installation A must not affect installation B even
        // when they share ClientApp. FR-006 + data-model invariant 5.
        using HttpClient unauthed = _factory.CreateClient();
        using HttpClient admin = AdminClient();

        (int idA, _) = await RegisterInstallationAsync(unauthed,
            "ButtonPanelTester", "userA", "machineA",
            Guid.Parse("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            "stbt_iso-a");
        (int idB, string plaintextB) = await RegisterInstallationAsync(unauthed,
            "ButtonPanelTester", "userB", "machineB",
            Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef0123456789"),
            "stbt_iso-b");
        Assert.NotEqual(idA, idB);

        HttpResponseMessage revoke = await admin.PostAsync(
            $"/api/admin/installations/{idA}/revoke", content: null);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        // B's credential continues to authenticate /api/dictionaries.
        using HttpRequestMessage authedB = new(HttpMethod.Get, "/api/dictionaries");
        authedB.Headers.Add("X-Api-Key", plaintextB);
        HttpResponseMessage usingB = await unauthed.SendAsync(authedB);
        Assert.Equal(HttpStatusCode.OK, usingB.StatusCode);
    }

    [Fact]
    public async Task Revoke_RevokedCredentialFailsWithin5s()
    {
        // T064: SC-004. After warming the validator cache via one successful
        // authenticated call, a subsequent revoke MUST invalidate the cache
        // entry so the very next call fails — well under the 5 s ceiling.
        using HttpClient unauthed = _factory.CreateClient();
        using HttpClient admin = AdminClient();

        (int id, string plaintext) = await RegisterInstallationAsync(unauthed,
            "ButtonPanelTester", "u", "m",
            Guid.Parse("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            "stbt_revoke-5s");

        // Warm the validator cache.
        using HttpRequestMessage warmup = new(HttpMethod.Get, "/api/dictionaries");
        warmup.Headers.Add("X-Api-Key", plaintext);
        Assert.Equal(HttpStatusCode.OK, (await unauthed.SendAsync(warmup)).StatusCode);

        HttpResponseMessage revoke = await admin.PostAsync(
            $"/api/admin/installations/{id}/revoke", content: null);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        DateTime startedAt = DateTime.UtcNow;
        using HttpRequestMessage afterRevoke = new(HttpMethod.Get, "/api/dictionaries");
        afterRevoke.Headers.Add("X-Api-Key", plaintext);
        HttpResponseMessage rejected = await unauthed.SendAsync(afterRevoke);
        TimeSpan elapsed = DateTime.UtcNow - startedAt;

        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        Assert.True(elapsed < TimeSpan.FromSeconds(1),
            $"Revoked credential must be rejected immediately via cache invalidation; took {elapsed}.");
    }

    [Fact]
    public async Task Revoke_IsIdempotent()
    {
        // T065: second revoke returns 200 with the original revokedAt, no
        // second audit row, no second credential row mutation.
        using HttpClient unauthed = _factory.CreateClient();
        using HttpClient admin = AdminClient();

        (int id, _) = await RegisterInstallationAsync(unauthed,
            "ButtonPanelTester", "u", "m",
            Guid.Parse("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            "stbt_idem");

        HttpResponseMessage first = await admin.PostAsync(
            $"/api/admin/installations/{id}/revoke", content: null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using JsonDocument firstBody = await ReadJsonAsync(first);
        DateTime firstRevokedAt = firstBody.RootElement.GetProperty("revokedAt")
            .GetDateTime().ToUniversalTime();

        // Second revoke.
        HttpResponseMessage second = await admin.PostAsync(
            $"/api/admin/installations/{id}/revoke", content: null);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using JsonDocument secondBody = await ReadJsonAsync(second);
        DateTime secondRevokedAt = secondBody.RootElement.GetProperty("revokedAt")
            .GetDateTime().ToUniversalTime();
        Assert.Equal(firstRevokedAt, secondRevokedAt);

        // No second audit row.
        await using AppDbContext db = _factory.NewContext();
        AuditEntryEntity[] entries = await db.AuditEntries.AsNoTracking()
            .Where(a => a.EntityType == AuditEntityType.Installation && a.EntityId == id)
            .ToArrayAsync();
        Assert.Single(entries);
    }

    [Fact]
    public async Task Revoke_NotFound_Returns404()
    {
        // T066 part 1: unknown installationId.
        using HttpClient admin = AdminClient();

        HttpResponseMessage response = await admin.PostAsync(
            "/api/admin/installations/9999999/revoke", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument body = await ReadJsonAsync(response);
        Assert.Equal("installation not found",
            body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Revoke_AuditEntryWritten()
    {
        // T066 part 2: first revoke writes one AuditEntry via
        // LogUpdateAsync(EntityType=Installation, EntityId=<id>,
        // ChangedById=<system-admin>, Active→Revoked payload).
        using HttpClient unauthed = _factory.CreateClient();
        using HttpClient admin = AdminClient();

        (int id, _) = await RegisterInstallationAsync(unauthed,
            "ButtonPanelTester", "u", "m",
            Guid.Parse("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            "stbt_audit");

        HttpResponseMessage revoke = await admin.PostAsync(
            $"/api/admin/installations/{id}/revoke", content: null);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        await using AppDbContext db = _factory.NewContext();
        UserEntity adminUser = await db.Users.SingleAsync(u => u.Username == "system-admin");
        AuditEntryEntity[] entries = await db.AuditEntries.AsNoTracking()
            .Where(a => a.EntityType == AuditEntityType.Installation && a.EntityId == id)
            .ToArrayAsync();
        Assert.Single(entries);
        AuditEntryEntity entry = entries[0];
        Assert.Equal(AuditOperation.Update, entry.Operation);
        Assert.Equal(adminUser.Id, entry.ChangedById);
        Assert.NotNull(entry.PreviousValue);
        Assert.NotNull(entry.NewValue);
        Assert.Contains("Active", entry.PreviousValue);
        Assert.Contains("Revoked", entry.NewValue);
    }

    [Fact]
    public async Task List_WithoutAdminKey_Returns401()
    {
        // Sanity: admin gate must reject non-admin keys, like the bootstrap-token
        // admin endpoint does.
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", AdminAuthApiFactory.NonAdminKey);

        HttpResponseMessage response = await client.GetAsync("/api/admin/installations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
