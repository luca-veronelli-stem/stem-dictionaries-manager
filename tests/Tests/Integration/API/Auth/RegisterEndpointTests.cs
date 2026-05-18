using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Infrastructure.Repositories.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.Auth;
using Services.Interfaces.Auth;

namespace Tests.Integration.API.Auth;

/// <summary>
/// HTTP-level integration tests for <c>POST /register</c>. Spins the API
/// up via <see cref="RegisterApiFactory"/> against a per-test SQLite
/// in-memory DB so the per-outcome status mapping (narrowed FR-002 /
/// SC-002) and the FR-005 union-mode credential round-trip can be
/// exercised end-to-end.
/// </summary>
public class RegisterEndpointTests : IDisposable
{
    /// <summary>
    /// The shared failure body envelope — same bytes across all failure
    /// statuses (400 / 401 / 409 / 410 / 423). The status code distinguishes
    /// the failure class; the body stays identical so clients cannot use it
    /// as a token-validity oracle for the three scope-related 401 modes.
    /// </summary>
    private const string FailureBody = "{\"error\":\"registration failed\"}";

    private readonly RegisterApiFactory _factory = new();
    private readonly PasswordHasher _hasher = new();

    private async Task<BootstrapTokenEntity> SeedActiveTokenAsync(string clientApp,
        string plaintext, TimeSpan? ttl = null)
    {
        DateTime mintedAt = DateTime.UtcNow.AddSeconds(-30);
        BootstrapTokenEntity entity = new()
        {
            ClientApp = clientApp,
            SecretHash = _hasher.Hash(plaintext),
            MintedAt = mintedAt,
            ExpiresAt = mintedAt + (ttl ?? TimeSpan.FromDays(30)),
            Status = BootstrapTokenStatus.Issued
        };
        await using AppDbContext db = _factory.NewContext();
        db.BootstrapTokens.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    private static StringContent JsonBody(object payload)
        => new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static object ValidDescriptor(string clientApp = "ButtonPanelTester")
        => new
        {
            clientApp,
            osUserId = "S-1-5-21-2127521184-1604012920-1887927527-72713",
            machineId = "8a5e9b3c-6f4d-4d2a-9c1b-7d8e3f4b6c2a",
            installGuid = "f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c",
            appVersion = "1.0.0"
        };

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response)
        => await response.Content.ReadAsStringAsync();

    [Fact]
    public async Task Register_WithValidToken_Returns200AndCredentialShape()
    {
        BootstrapTokenEntity token = await SeedActiveTokenAsync("ButtonPanelTester", "stbt_valid-1");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = "stbt_valid-1", descriptor = ValidDescriptor() }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await ReadBodyAsync(response));
        Assert.True(doc.RootElement.GetProperty("installationId").GetInt32() > 0);
        string apiCredential = doc.RootElement.GetProperty("apiCredential").GetString()!;
        Assert.StartsWith("stak_", apiCredential);
        DateTime issuedAt = doc.RootElement.GetProperty("issuedAt").GetDateTime();
        Assert.True(DateTime.UtcNow.Subtract(issuedAt) < TimeSpan.FromMinutes(1));

        // Token row was transitioned in the same transaction.
        await using AppDbContext db = _factory.NewContext();
        BootstrapTokenEntity? rotated = await db.BootstrapTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == token.Id);
        Assert.NotNull(rotated);
        Assert.Equal(BootstrapTokenStatus.Used, rotated!.Status);
        Assert.NotNull(rotated.ConsumedByInstallationId);
    }

    [Fact]
    public async Task Register_ReturnedCredentialAuthenticatesProtectedEndpoint()
    {
        // FR-005: the freshly issued credential must work via the union-mode
        // ApiKeyMiddleware on a subsequent request to /api/dictionaries.
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_valid-2");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage register = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = "stbt_valid-2", descriptor = ValidDescriptor() }));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        using var doc = JsonDocument.Parse(await ReadBodyAsync(register));
        string credential = doc.RootElement.GetProperty("apiCredential").GetString()!;

        using HttpRequestMessage authedRequest = new(HttpMethod.Get, "/api/dictionaries");
        authedRequest.Headers.Add("X-Api-Key", credential);
        HttpResponseMessage protectedResponse = await client.SendAsync(authedRequest);

        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Theory]
    [MemberData(nameof(FailurePayloads))]
    public async Task Register_FailureModes_ReturnExpectedStatusAndFailureBody(
        string scenarioName, object? payload, bool seedToken, HttpStatusCode expectedStatus)
    {
        // Narrowed SC-002: failure bodies share the same envelope; the status
        // code distinguishes the failure class.
        if (seedToken)
        {
            await SeedActiveTokenAsync("ButtonPanelTester", "stbt_seeded");
        }
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = payload is string raw
            ? await client.PostAsync("/register",
                new StringContent(raw, Encoding.UTF8, "application/json"))
            : await client.PostAsync("/register", JsonBody(payload!));

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FailureBody, await ReadBodyAsync(response));
        _ = scenarioName;
    }

    public static IEnumerable<object?[]> FailurePayloads()
    {
        // Token issues — TokenMissing is a client bug (400); UnknownToken
        // and ClientScopeMismatch collapse to 401 to hide token scope.
        yield return new object?[] { "MissingToken",
            new { descriptor = ValidDescriptor() }, false, HttpStatusCode.BadRequest };
        yield return new object?[] { "EmptyToken",
            new { bootstrapToken = "", descriptor = ValidDescriptor() }, false, HttpStatusCode.BadRequest };
        yield return new object?[] { "UnknownToken",
            new { bootstrapToken = "stbt_nonexistent", descriptor = ValidDescriptor() }, true,
            HttpStatusCode.Unauthorized };
        yield return new object?[] { "ClientScopeMismatch",
            new { bootstrapToken = "stbt_seeded", descriptor = ValidDescriptor("GlobalService") }, true,
            HttpStatusCode.Unauthorized };
        // Descriptor issues — all currently surface as DescriptorMalformed (400).
        // ZeroInstallGuid splits into InstallGuidInvalid in a follow-up commit;
        // MissingMachineId splits into DescriptorMissingField when the per-clientApp
        // policy lands. Status remains 400 in either case.
        yield return new object?[] { "ZeroInstallGuid",
            new
            {
                bootstrapToken = "stbt_seeded",
                descriptor = new
                {
                    clientApp = "ButtonPanelTester",
                    osUserId = "u",
                    machineId = "m",
                    installGuid = "00000000-0000-0000-0000-000000000000",
                    appVersion = "1.0.0"
                }
            },
            true, HttpStatusCode.BadRequest };
        yield return new object?[] { "MissingMachineId",
            new
            {
                bootstrapToken = "stbt_seeded",
                descriptor = new
                {
                    clientApp = "ButtonPanelTester",
                    osUserId = "u",
                    installGuid = "f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"
                }
            },
            true, HttpStatusCode.BadRequest };
        yield return new object?[] { "MalformedJson", "{ not really json", false,
            HttpStatusCode.BadRequest };
    }

    [Fact]
    public async Task Register_ZeroInstallGuid_Returns400AndAuditRowCarriesInstallGuidInvalid()
    {
        // Distinct outcome (vs DescriptorMalformed): a buggy client hardcoded
        // to Guid.Empty gets a clean 400 surface, and the audit log records
        // the specific outcome for ops forensics.
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_zero-guid");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new
            {
                bootstrapToken = "stbt_zero-guid",
                descriptor = new
                {
                    clientApp = "ButtonPanelTester",
                    osUserId = "u",
                    machineId = "m",
                    installGuid = "00000000-0000-0000-0000-000000000000",
                    appVersion = "1.0.0"
                }
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(FailureBody, await ReadBodyAsync(response));

        await using AppDbContext db = _factory.NewContext();
        RegistrationEventEntity evt = await db.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.InstallGuidInvalid, evt.Outcome);
    }

    [Fact]
    public async Task Register_MissingMachineIdForStrictPolicy_Returns400AndAuditRowCarriesDescriptorMissingField()
    {
        // ButtonPanelTester is registered (in Services/DependencyInjection)
        // as strict; omitting machineId surfaces as DescriptorMissingField.
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_missing-machine");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new
            {
                bootstrapToken = "stbt_missing-machine",
                descriptor = new
                {
                    clientApp = "ButtonPanelTester",
                    osUserId = "u",
                    installGuid = "f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c",
                    appVersion = "1.0.0"
                }
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(FailureBody, await ReadBodyAsync(response));

        await using AppDbContext db = _factory.NewContext();
        RegistrationEventEntity evt = await db.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.DescriptorMissingField, evt.Outcome);
    }

    [Fact]
    public async Task Register_UnknownClientApp_Returns401AndAuditRowCarriesClientScopeMismatch()
    {
        // The policy registry only contains ButtonPanelTester; a request whose
        // clientApp is anything else fails the policy lookup and gets the
        // conflated 401 (same wire response as token-unknown / scope-mismatch).
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_unknown-clientApp");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new
            {
                bootstrapToken = "stbt_unknown-clientApp",
                descriptor = new
                {
                    clientApp = "UnregisteredApp",
                    osUserId = "u",
                    machineId = "m",
                    installGuid = "f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c",
                    appVersion = "1.0.0"
                }
            }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(FailureBody, await ReadBodyAsync(response));

        await using AppDbContext db = _factory.NewContext();
        RegistrationEventEntity evt = await db.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.ClientScopeMismatch, evt.Outcome);
    }

    [Fact]
    public async Task Register_ExpiredToken_Returns410WithFailureBody()
    {
        // Seed a token that's already past its ExpiresAt — domain TTL constraint
        // requires ttl >= 1h, so MintedAt is set far enough in the past for both
        // the construction invariant and the IsExpiredAt check to be satisfied.
        DateTime past = DateTime.UtcNow.AddDays(-7);
        await using (AppDbContext db = _factory.NewContext())
        {
            db.BootstrapTokens.Add(new BootstrapTokenEntity
            {
                ClientApp = "ButtonPanelTester",
                SecretHash = _hasher.Hash("stbt_expired"),
                MintedAt = past,
                ExpiresAt = past.AddHours(1),
                Status = BootstrapTokenStatus.Issued
            });
            await db.SaveChangesAsync();
        }
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = "stbt_expired", descriptor = ValidDescriptor() }));

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Equal(FailureBody, await ReadBodyAsync(response));
    }

    [Fact]
    public async Task Register_SameTokenReplayed_OnlyFirstCallSucceeds()
    {
        // Single-use enforcement (SC-003 spirit): once a token is consumed, any
        // subsequent attempt returns 401 — the lookup ignores Used rows so the
        // second call surfaces as TokenInvalid (conflated with unknown-token).
        // The race-loser path which can observe the token in Used state directly
        // surfaces TokenAlreadyUsed -> 409 (exercised separately below).
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_single-use");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage first = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = "stbt_single-use", descriptor = ValidDescriptor() }));
        HttpResponseMessage second = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = "stbt_single-use", descriptor = ValidDescriptor() }));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(FailureBody, await ReadBodyAsync(second));

        // Exactly one Installation row exists with the FK back-pointing.
        await using AppDbContext db = _factory.NewContext();
        Assert.Equal(1, await db.Installations.CountAsync());
        Assert.Equal(1, await db.InstallationApiCredentials.CountAsync());
        BootstrapTokenEntity[] tokens = await db.BootstrapTokens.AsNoTracking().ToArrayAsync();
        Assert.Single(tokens);
        Assert.Equal(BootstrapTokenStatus.Used, tokens[0].Status);
    }

    [Fact]
    public async Task Register_ConcurrentRaceLoser_Returns409WithFailureBodyAndNoInstallation()
    {
        // SC-003: when MarkUsedAsync throws BootstrapTokenStateException — the
        // canonical race-loser signal — the endpoint must respond with 409
        // (TokenAlreadyUsed; not 500, not the conflated 401) and leave no
        // Installation row behind.
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_race-http");
        _factory.BootstrapTokenSvcFactory = sp => new RaceLosingBootstrapTokenServiceForHttp(
            sp.GetRequiredService<AppDbContext>(), _hasher);
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = "stbt_race-http", descriptor = ValidDescriptor() }));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FailureBody, await ReadBodyAsync(response));

        await using AppDbContext db = _factory.NewContext();
        Assert.Equal(0, await db.Installations.CountAsync());
        Assert.Equal(0, await db.InstallationApiCredentials.CountAsync());
        // The audit row exists and is forensically TokenAlreadyUsed.
        RegistrationEventEntity evt = await db.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenAlreadyUsed, evt.Outcome);
    }

    [Fact]
    public async Task Register_AuditWriteFailure_Returns500AndPersistsNoInstallation()
    {
        // FR-013: when the audit row write throws, return 500 with the audit
        // failure body and ensure no Installation/credential is persisted —
        // the endpoint must not falsely tell the client their token is bad.
        _factory.EventRepoOverride = new ThrowingEventRepository();
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_audit-fail");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = "stbt_audit-fail", descriptor = ValidDescriptor() }));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("{\"error\":\"audit failure\"}", await ReadBodyAsync(response));

        await using AppDbContext db = _factory.NewContext();
        Assert.Equal(0, await db.Installations.CountAsync());
        Assert.Equal(0, await db.InstallationApiCredentials.CountAsync());
    }

    [Fact]
    public async Task Register_PlaintextCredentialNotEchoedBackInRequest()
    {
        // SC-007 invariant 4 surface check: the plaintext returned in the body
        // is not the same string the client submitted. (Full log scanning lives
        // in a follow-up — there's no app-level logger writing the credential
        // anywhere on the success path, so this end-to-end check is the
        // strongest assertion we can make from outside the process.)
        await SeedActiveTokenAsync("ButtonPanelTester", "stbt_plaintext-check");
        using HttpClient client = _factory.CreateClient();
        const string requestToken = "stbt_plaintext-check";

        HttpResponseMessage response = await client.PostAsync("/register",
            JsonBody(new { bootstrapToken = requestToken, descriptor = ValidDescriptor() }));

        string body = await ReadBodyAsync(response);
        Assert.DoesNotContain(requestToken, body);
        using var bodyDoc = JsonDocument.Parse(body);
        string credential = bodyDoc.RootElement.GetProperty("apiCredential").GetString()!;
        Assert.NotEqual(requestToken, credential);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal sealed class ThrowingEventRepository : IRegistrationEventRepository
{
    public Task<RegistrationEventEntity> AddAsync(RegistrationEventEntity entity,
        CancellationToken ct = default)
        => throw new InvalidOperationException("audit-write failure (test-injected)");

    public Task<IReadOnlyList<RegistrationEventEntity>> ListBySourceAsync(string sourceIp,
        DateTime since, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RegistrationEventEntity>>([]);
}

/// <summary>
/// HTTP-test fake: <see cref="LookupAsync"/> finds the seeded Issued token
/// (so the request reaches CommitSuccessAsync), then <see cref="MarkUsedAsync"/>
/// throws <see cref="BootstrapTokenStateException"/> as the canonical
/// race-loser signal.
/// </summary>
internal sealed class RaceLosingBootstrapTokenServiceForHttp : IBootstrapTokenService
{
    private readonly BootstrapTokenService _real;

    public RaceLosingBootstrapTokenServiceForHttp(AppDbContext db, PasswordHasher hasher)
    {
        _real = new BootstrapTokenService(new BootstrapTokenRepository(db), hasher);
    }

    public Task<BootstrapToken?> LookupAsync(string plaintext, CancellationToken ct = default)
        => _real.LookupAsync(plaintext, ct);

    public Task<(BootstrapToken Record, string Plaintext)> MintAsync(string clientApp,
        TimeSpan? ttl, CancellationToken ct = default)
        => _real.MintAsync(clientApp, ttl, ct);

    public Task MarkUsedAsync(int tokenId, int installationId, DateTime usedAt,
        CancellationToken ct = default)
        => throw new BootstrapTokenStateException(BootstrapTokenStatus.Used,
            "Race-loser HTTP test fake.");
}
