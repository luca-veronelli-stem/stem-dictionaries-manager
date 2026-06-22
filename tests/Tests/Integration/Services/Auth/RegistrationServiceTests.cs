using System.Collections.Generic;
using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Infrastructure.Repositories.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Services.Auth;
using Services.Interfaces.Auth;

namespace Tests.Integration.Services.Auth;

public class RegistrationServiceTests : IntegrationTestBase
{
    private readonly PasswordHasher _hasher = new();
    private readonly TokenGenerator _generator = new();
    private readonly FakeTimeProvider _time = new(
        new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc));

    /// <summary>
    /// Default policy registry used by every test that doesn't override it:
    /// the production-registered ButtonPanelTester strict policy. Loose-
    /// policy tests pass an explicit override.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, DescriptorPolicy> DefaultPolicies =
        new Dictionary<string, DescriptorPolicy>(StringComparer.Ordinal)
        {
            ["ButtonPanelTester"] = new(OsUserIdRequired: true, MachineIdRequired: true),
        };

    private RegistrationService BuildSut(IRegistrationEventRepository? eventsOverride = null,
        IBootstrapTokenService? bootstrapSvcOverride = null,
        IReadOnlyDictionary<string, DescriptorPolicy>? policiesOverride = null)
    {
        var bootstrapRepo = new BootstrapTokenRepository(Context);
        var installationRepo = new InstallationRepository(Context);
        var credentialRepo = new InstallationApiCredentialRepository(Context);
        IRegistrationEventRepository eventsRepo = eventsOverride
            ?? new RegistrationEventRepository(Context);

        IBootstrapTokenService bootstrapSvc = bootstrapSvcOverride
            ?? new BootstrapTokenService(bootstrapRepo, _hasher, NullLogger<BootstrapTokenService>.Instance);
        var credentialSvc = new InstallationCredentialService(credentialRepo, _generator, _hasher, NullLogger<InstallationCredentialService>.Instance);

        return new RegistrationService(Context, bootstrapSvc, credentialSvc,
            installationRepo, eventsRepo, policiesOverride ?? DefaultPolicies,
            NullLogger<RegistrationService>.Instance, _time);
    }

    private async Task<BootstrapTokenEntity> SeedTokenAsync(
        string clientApp, string plaintext,
        TimeSpan? ttl = null, BootstrapTokenStatus status = BootstrapTokenStatus.Issued)
    {
        DateTime mintedAt = _time.GetUtcNow().UtcDateTime;
        BootstrapTokenEntity entity = new()
        {
            ClientApp = clientApp,
            SecretHash = _hasher.Hash(plaintext),
            MintedAt = mintedAt,
            ExpiresAt = mintedAt + (ttl ?? TimeSpan.FromDays(30)),
            Status = status
        };
        Context.BootstrapTokens.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    private static RegisterRequest BuildRequest(string? token, string clientApp = "ButtonPanelTester")
        => new(
            BootstrapTokenPlaintext: token,
            ClientApp: clientApp,
            OsUserId: "S-1-5-21-2127521184-1604012920-1887927527-72713",
            MachineId: "8a5e9b3c-6f4d-4d2a-9c1b-7d8e3f4b6c2a",
            InstallGuid: new Guid("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c"),
            AppVersion: "1.0.0",
            DescriptorJson: "{\"clientApp\":\"" + clientApp + "\"}",
            SourceIp: "127.0.0.1");

    [Fact]
    public async Task RegisterAsync_ValidToken_PersistsInstallationCredentialAndAudit()
    {
        const string plaintext = "stbt_valid-token";
        BootstrapTokenEntity tokenEntity = await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Success success = Assert.IsType<RegistrationResult.Success>(result);
        Assert.StartsWith("stak_", success.ApiCredentialPlaintext);
        Assert.True(success.InstallationId > 0);

        InstallationEntity? install = await Context.Installations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == success.InstallationId);
        Assert.NotNull(install);
        Assert.Equal(InstallationStatus.Active, install!.Status);
        Assert.Equal("ButtonPanelTester", install.ClientApp);

        InstallationApiCredentialEntity? cred = await Context.InstallationApiCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.InstallationId == success.InstallationId);
        Assert.NotNull(cred);
        Assert.Equal(InstallationStatus.Active, cred!.Status);
        Assert.True(_hasher.Verify(success.ApiCredentialPlaintext, cred.SecretHash));

        BootstrapTokenEntity? rotatedToken = await Context.BootstrapTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenEntity.Id);
        Assert.NotNull(rotatedToken);
        Assert.Equal(BootstrapTokenStatus.Used, rotatedToken!.Status);
        Assert.Equal(success.InstallationId, rotatedToken.ConsumedByInstallationId);

        RegistrationEventEntity? evt = await Context.RegistrationEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ResultingInstallationId == success.InstallationId);
        Assert.NotNull(evt);
        Assert.Equal(RegistrationOutcome.Success, evt!.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_TokenMissing_FailsWithTokenMissingOutcomeAndNoInstallation()
    {
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(token: null));

        Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(0, await Context.Installations.CountAsync());
        Assert.Equal(0, await Context.InstallationApiCredentials.CountAsync());

        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenMissing, evt.Outcome);
        Assert.Null(evt.ResultingInstallationId);
    }

    [Fact]
    public async Task RegisterAsync_UnknownToken_FailsWithTokenInvalidOutcome()
    {
        await SeedTokenAsync("ButtonPanelTester", "stbt_real-token");
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest("stbt_unknown-token"));

        Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(0, await Context.Installations.CountAsync());
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenInvalid, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_ExpiredToken_FailsAndDoesNotMarkUsed()
    {
        const string plaintext = "stbt_expiring-token";
        BootstrapTokenEntity tokenEntity = await SeedTokenAsync("ButtonPanelTester", plaintext,
            ttl: TimeSpan.FromHours(1));
        // Advance the clock past the token's TTL.
        _time.Advance(TimeSpan.FromHours(2));
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        Assert.IsType<RegistrationResult.Failure>(result);
        BootstrapTokenEntity? unchanged = await Context.BootstrapTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenEntity.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(BootstrapTokenStatus.Issued, unchanged!.Status);
        Assert.Null(unchanged.UsedAt);

        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenExpired, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_ClientScopeMismatch_FailsWithMismatchOutcome()
    {
        const string plaintext = "stbt_scoped-token";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(
            BuildRequest(plaintext, clientApp: "GlobalService"));

        Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(0, await Context.Installations.CountAsync());
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.ClientScopeMismatch, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_DescriptorWithZeroGuid_FailsWithInstallGuidInvalid()
    {
        // Guid.Empty has its own outcome so a buggy client surfaces the
        // problem on the first attempt (400) instead of via the unique-index
        // collision on the second attempt (would otherwise be a confusing 500).
        const string plaintext = "stbt_token-3";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegisterRequest request = BuildRequest(plaintext) with { InstallGuid = Guid.Empty };
        RegistrationResult result = await sut.RegisterAsync(request);

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.InstallGuidInvalid, failure.Outcome);
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.InstallGuidInvalid, evt.Outcome);
    }

    [Theory]
    [InlineData("banana")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("v1.0.0")]
    [InlineData("01.0.0")]
    public async Task RegisterAsync_DescriptorWithInvalidAppVersion_FailsWithDescriptorMalformed(
        string appVersion)
    {
        // SemVer 2.0 grammar enforced at the service layer (so the unified
        // {"error":"..."} envelope from FR-002 stays consistent vs an ASP.NET
        // model-validation 400 with a different body shape).
        string plaintext = $"stbt_semver-invalid-{appVersion.GetHashCode()}";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegisterRequest request = BuildRequest(plaintext) with { AppVersion = appVersion };
        RegistrationResult result = await sut.RegisterAsync(request);

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.DescriptorMalformed, failure.Outcome);
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.DescriptorMalformed, evt.Outcome);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("0.0.1")]
    [InlineData("10.20.30")]
    [InlineData("1.0.0-alpha")]
    [InlineData("1.0.0-alpha.1")]
    [InlineData("1.0.0+20260518")]
    [InlineData("1.0.0-rc.1+build.42")]
    [InlineData(" 1.0.0 ")] // whitespace trimmed before matching
    public async Task RegisterAsync_DescriptorWithValidSemVer_Succeeds(string appVersion)
    {
        string plaintext = $"stbt_semver-valid-{appVersion.GetHashCode()}";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegisterRequest request = BuildRequest(plaintext) with { AppVersion = appVersion };
        RegistrationResult result = await sut.RegisterAsync(request);

        Assert.IsType<RegistrationResult.Success>(result);
    }

    [Fact]
    public async Task RegisterAsync_StrictPolicy_RejectsMissingMachineIdWithDescriptorMissingField()
    {
        // ButtonPanelTester is registered as strict (MachineIdRequired=true).
        // A request that omits MachineId surfaces as DescriptorMissingField
        // (distinct from DescriptorMalformed) -> 400.
        const string plaintext = "stbt_token-4";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegisterRequest request = BuildRequest(plaintext) with { MachineId = "" };
        RegistrationResult result = await sut.RegisterAsync(request);

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.DescriptorMissingField, failure.Outcome);
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.DescriptorMissingField, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_LoosePolicy_AcceptsMissingMachineIdAndStoresNull()
    {
        // A loose-policy consumer (here represented by "MobileApp" registered
        // with MachineIdRequired=false) is allowed to omit machineId; storage
        // records null.
        const string plaintext = "stbt_loose-machine";
        await SeedTokenAsync("MobileApp", plaintext);
        IReadOnlyDictionary<string, DescriptorPolicy> loosePolicies =
            new Dictionary<string, DescriptorPolicy>(StringComparer.Ordinal)
            {
                ["MobileApp"] = new(OsUserIdRequired: true, MachineIdRequired: false),
            };
        RegistrationService sut = BuildSut(policiesOverride: loosePolicies);

        RegisterRequest request = BuildRequest(plaintext, clientApp: "MobileApp") with
        {
            MachineId = ""
        };
        RegistrationResult result = await sut.RegisterAsync(request);

        Assert.IsType<RegistrationResult.Success>(result);
        InstallationEntity installation = await Context.Installations.AsNoTracking().SingleAsync();
        Assert.Null(installation.MachineId);
        Assert.NotNull(installation.OsUserId);
    }

    [Fact]
    public async Task RegisterAsync_LoosePolicy_AcceptsMissingBothAndStoresNulls()
    {
        // Headless-style consumer: neither OsUserId nor MachineId required.
        const string plaintext = "stbt_loose-both";
        await SeedTokenAsync("HeadlessService", plaintext);
        IReadOnlyDictionary<string, DescriptorPolicy> loosePolicies =
            new Dictionary<string, DescriptorPolicy>(StringComparer.Ordinal)
            {
                ["HeadlessService"] = new(OsUserIdRequired: false, MachineIdRequired: false),
            };
        RegistrationService sut = BuildSut(policiesOverride: loosePolicies);

        RegisterRequest request = BuildRequest(plaintext, clientApp: "HeadlessService") with
        {
            OsUserId = null,
            MachineId = null
        };
        RegistrationResult result = await sut.RegisterAsync(request);

        Assert.IsType<RegistrationResult.Success>(result);
        InstallationEntity installation = await Context.Installations.AsNoTracking().SingleAsync();
        Assert.Null(installation.OsUserId);
        Assert.Null(installation.MachineId);
    }

    [Fact]
    public async Task RegisterAsync_UnknownClientApp_ConflatesAs401ClientScopeMismatch()
    {
        // ClientApp = "UnregisteredApp" -- not in the policy registry. The
        // lookup-miss conflates into ClientScopeMismatch (-> 401), hiding
        // which apps the token was scoped to.
        const string plaintext = "stbt_unknown-clientApp";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegisterRequest request = BuildRequest(plaintext, clientApp: "UnregisteredApp");
        RegistrationResult result = await sut.RegisterAsync(request);

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.ClientScopeMismatch, failure.Outcome);
        // The audit row carries the same outcome -- ops cannot distinguish
        // unknown-clientApp from scope-mismatch from token-unknown via the
        // outcome enum here; that's by design (the conflation is end-to-end).
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.ClientScopeMismatch, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_PreviouslyConsumedToken_FailsWithTokenAlreadyUsed()
    {
        // #58 non-race path: a Used row is visible to LookupAsync and
        // ClassifyOutcome branches it directly to TokenAlreadyUsed (-> 409),
        // distinct from the conflated 401 it used to receive.
        const string plaintext = "stbt_consumed";
        await SeedTokenAsync("ButtonPanelTester", plaintext,
            status: BootstrapTokenStatus.Used);
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.TokenAlreadyUsed, failure.Outcome);
        Assert.Equal(0, await Context.Installations.CountAsync());
        Assert.Equal(0, await Context.InstallationApiCredentials.CountAsync());

        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenAlreadyUsed, evt.Outcome);
        Assert.Null(evt.ResultingInstallationId);
    }

    [Fact]
    public async Task RegisterAsync_RevokedToken_FailsWithTokenRevoked()
    {
        // #58 non-race path: a Revoked row maps to TokenRevoked (-> 423),
        // forensically distinct from the consumed (Used) case.
        const string plaintext = "stbt_revoked";
        await SeedTokenAsync("ButtonPanelTester", plaintext,
            status: BootstrapTokenStatus.Revoked);
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.TokenRevoked, failure.Outcome);
        Assert.Equal(0, await Context.Installations.CountAsync());

        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenRevoked, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_UsedTokenAlsoExpired_FailsWithTokenAlreadyUsedNotExpired()
    {
        // Ordering check (#58): when a token is both Used and past its TTL,
        // TokenAlreadyUsed wins over TokenExpired -- the actionable user-facing
        // message is "you already registered with this token", not "it expired".
        const string plaintext = "stbt_used-and-expired";
        BootstrapTokenEntity entity = await SeedTokenAsync("ButtonPanelTester", plaintext,
            ttl: TimeSpan.FromHours(1),
            status: BootstrapTokenStatus.Used);
        _time.Advance(TimeSpan.FromHours(2));
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.TokenAlreadyUsed, failure.Outcome);
        _ = entity;
    }

    [Fact]
    public async Task RegisterAsync_RaceLoserAfterLookup_ReturnsFailureAndRollsBackInstall()
    {
        // SC-003 / data-model invariant 1: when a concurrent /register on the
        // same token wins the MarkUsed race, this caller's MarkUsedAsync
        // throws BootstrapTokenStateException(FoundStatus=Used). RegistrationService
        // must roll back the in-flight install + credential and emit a
        // TokenAlreadyUsed audit so the endpoint responds with the unified 401.
        const string plaintext = "stbt_race-token";
        BootstrapTokenEntity tokenEntity = await SeedTokenAsync("ButtonPanelTester", plaintext);

        BootstrapTokenService realSvc = new(new BootstrapTokenRepository(Context), _hasher,
            NullLogger<BootstrapTokenService>.Instance);
        RaceLosingBootstrapTokenService raceSvc = new(realSvc,
            throwWith: BootstrapTokenStatus.Used);
        RegistrationService sut = BuildSut(bootstrapSvcOverride: raceSvc);

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(0, await Context.Installations.CountAsync());
        Assert.Equal(0, await Context.InstallationApiCredentials.CountAsync());
        // Token row is intact (rolled back; the simulated winner would have
        // committed in a real concurrent flow but this test only exercises the
        // loser's path).
        BootstrapTokenEntity? unchanged = await Context.BootstrapTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenEntity.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(BootstrapTokenStatus.Issued, unchanged!.Status);

        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenAlreadyUsed, evt.Outcome);
        Assert.Null(evt.ResultingInstallationId);
    }

    [Fact]
    public async Task RegisterAsync_RaceLoserOnRevokedToken_AuditsAsTokenRevoked()
    {
        // FoundStatus=Revoked must surface as TokenRevoked in the audit row
        // (still 401 to the client, but forensically distinct from AlreadyUsed).
        const string plaintext = "stbt_race-revoked";
        await SeedTokenAsync("ButtonPanelTester", plaintext);

        BootstrapTokenService realSvc = new(new BootstrapTokenRepository(Context), _hasher,
            NullLogger<BootstrapTokenService>.Instance);
        RaceLosingBootstrapTokenService raceSvc = new(realSvc,
            throwWith: BootstrapTokenStatus.Revoked);
        RegistrationService sut = BuildSut(bootstrapSvcOverride: raceSvc);

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        Assert.IsType<RegistrationResult.Failure>(result);
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenRevoked, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_AuditWriteFailureOnSuccessPath_RollsBackInstallationAndCredential()
    {
        // FR-013: a thrown audit-write must propagate up; the endpoint maps to
        // 500 with {"error":"audit failure"} and NO installation row remains.
        const string plaintext = "stbt_token-5";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut(
            eventsOverride: new ThrowingRegistrationEventRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.RegisterAsync(BuildRequest(plaintext)));

        Assert.Equal(0, await Context.Installations.CountAsync());
        Assert.Equal(0, await Context.InstallationApiCredentials.CountAsync());
        Assert.Equal(0, await Context.RegistrationEvents.CountAsync());
        // Token state must also be intact (transaction-rolled-back).
        BootstrapTokenEntity[] tokens = await Context.BootstrapTokens.AsNoTracking().ToArrayAsync();
        Assert.All(tokens, t => Assert.Equal(BootstrapTokenStatus.Issued, t.Status));
    }

    // ----- Re-registration rejection paths (#71 slice 5) -----

    private static readonly Guid ReRegInstallGuid =
        new("f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c");

    private async Task<InstallationEntity> SeedInstallationAsync(string clientApp,
        InstallationStatus status, Guid? installGuid = null)
    {
        InstallationEntity entity = new()
        {
            ClientApp = clientApp,
            OsUserId = "S-1-5-21-2127521184-1604012920-1887927527-72713",
            MachineId = "8a5e9b3c-6f4d-4d2a-9c1b-7d8e3f4b6c2a",
            InstallGuid = installGuid ?? ReRegInstallGuid,
            AppVersion = "1.0.0",
            DescriptorJson = "{}",
            RegisteredAt = _time.GetUtcNow().UtcDateTime,
            Status = status,
            RevokedAt = status == InstallationStatus.Revoked
                ? _time.GetUtcNow().UtcDateTime : null
        };
        Context.Installations.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task RegisterAsync_FreshTokenOnExistingInstallGuid_CrossApp_RoutesToClientScopeMismatch_NoMutation()
    {
        // Existing installation under a different ClientApp than the
        // request. Spec 002 FR-002: the conflated 401 path takes the
        // request; no row in Installations / InstallationApiCredentials
        // is mutated; only an audit row is written.
        InstallationEntity existing = await SeedInstallationAsync(
            clientApp: "GlobalService", status: InstallationStatus.Active);
        const string plaintext = "stbt_cross-app";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.ClientScopeMismatch, failure.Outcome);

        // Existing installation is untouched.
        InstallationEntity? after = await Context.Installations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == existing.Id);
        Assert.NotNull(after);
        Assert.Equal("GlobalService", after!.ClientApp);
        Assert.Equal(InstallationStatus.Active, after.Status);
        Assert.Equal(1, await Context.Installations.CountAsync());

        // No new credential issued.
        Assert.Equal(0, await Context.InstallationApiCredentials.CountAsync());

        // Token not consumed.
        BootstrapTokenEntity[] tokens = await Context.BootstrapTokens.AsNoTracking().ToArrayAsync();
        Assert.All(tokens, t => Assert.Equal(BootstrapTokenStatus.Issued, t.Status));

        // Audit row records the conflated-401 outcome.
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking()
            .SingleAsync();
        Assert.Equal(RegistrationOutcome.ClientScopeMismatch, evt.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_FreshTokenOnExistingInstallGuid_RevokedInstallation_RoutesToExistingInstallationRevoked_NoMutation()
    {
        // Existing installation is Revoked. Spec 002 FR-003: reject via
        // the conflated 401 path with the dedicated server-only outcome
        // ExistingInstallationRevoked. Installation must NOT be
        // auto-unrevoked.
        InstallationEntity existing = await SeedInstallationAsync(
            clientApp: "ButtonPanelTester", status: InstallationStatus.Revoked);
        const string plaintext = "stbt_revoked-install";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.ExistingInstallationRevoked, failure.Outcome);

        // Installation stays Revoked (not auto-unrevoked).
        InstallationEntity? after = await Context.Installations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == existing.Id);
        Assert.NotNull(after);
        Assert.Equal(InstallationStatus.Revoked, after!.Status);
        Assert.NotNull(after.RevokedAt);

        // No credential issued.
        Assert.Equal(0, await Context.InstallationApiCredentials.CountAsync());

        // Token not consumed.
        BootstrapTokenEntity[] tokens = await Context.BootstrapTokens.AsNoTracking().ToArrayAsync();
        Assert.All(tokens, t => Assert.Equal(BootstrapTokenStatus.Issued, t.Status));

        // Audit row records the dedicated server-only outcome.
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking()
            .SingleAsync();
        Assert.Equal(RegistrationOutcome.ExistingInstallationRevoked, evt.Outcome);
    }

    // ----- Re-registration happy path + race-loser (#71 slice 6) -----

    private async Task<InstallationApiCredentialEntity> SeedActiveCredentialAsync(
        InstallationEntity installation, string plaintext)
    {
        InstallationApiCredentialEntity entity = new()
        {
            InstallationId = installation.Id,
            Installation = installation,
            SecretHash = _hasher.Hash(plaintext),
            IssuedAt = _time.GetUtcNow().UtcDateTime,
            Status = InstallationStatus.Active
        };
        Context.InstallationApiCredentials.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task RegisterAsync_FreshTokenOnExistingActiveInstallation_RevokesPriorCredentialsIssuesNew_AuditsReRegistrationSuccess()
    {
        // Pre-existing installation + Active credential. A fresh
        // bootstrap token arrives. The atomic re-registration path
        // revokes the prior credential, issues a new one against the
        // same Installation row, and audits with ReRegistrationSuccess.
        InstallationEntity existing = await SeedInstallationAsync(
            clientApp: "ButtonPanelTester", status: InstallationStatus.Active);
        InstallationApiCredentialEntity priorCred =
            await SeedActiveCredentialAsync(existing, "stak_prior-cred");
        const string plaintext = "stbt_rereg-token";
        BootstrapTokenEntity token = await SeedTokenAsync("ButtonPanelTester", plaintext);
        DateTime now = _time.GetUtcNow().UtcDateTime;
        RegistrationService sut = BuildSut();

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Success success = Assert.IsType<RegistrationResult.Success>(result);
        Assert.Equal(existing.Id, success.InstallationId);
        Assert.StartsWith("stak_", success.ApiCredentialPlaintext);

        // Installation row is unchanged.
        InstallationEntity? installAfter = await Context.Installations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == existing.Id);
        Assert.NotNull(installAfter);
        Assert.Equal(InstallationStatus.Active, installAfter!.Status);
        Assert.Equal(1, await Context.Installations.CountAsync());

        // Prior credential is now Revoked with the now timestamp;
        // SecretHash is preserved.
        InstallationApiCredentialEntity? priorAfter = await Context.InstallationApiCredentials
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == priorCred.Id);
        Assert.NotNull(priorAfter);
        Assert.Equal(InstallationStatus.Revoked, priorAfter!.Status);
        Assert.Equal(now, priorAfter.RevokedAt);
        Assert.Equal(priorCred.SecretHash, priorAfter.SecretHash);

        // A new Active credential exists with a different SecretHash.
        InstallationApiCredentialEntity[] active = await Context.InstallationApiCredentials
            .AsNoTracking().Where(c => c.Status == InstallationStatus.Active).ToArrayAsync();
        Assert.Single(active);
        Assert.Equal(existing.Id, active[0].InstallationId);
        Assert.NotEqual(priorCred.SecretHash, active[0].SecretHash);
        Assert.True(_hasher.Verify(success.ApiCredentialPlaintext, active[0].SecretHash));

        // Plaintext changed.
        Assert.NotEqual("stak_prior-cred", success.ApiCredentialPlaintext);

        // Token transitioned to Used.
        BootstrapTokenEntity? tokenAfter = await Context.BootstrapTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == token.Id);
        Assert.NotNull(tokenAfter);
        Assert.Equal(BootstrapTokenStatus.Used, tokenAfter!.Status);
        Assert.Equal(existing.Id, tokenAfter.ConsumedByInstallationId);

        // Audit row uses the dedicated server-only outcome.
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking()
            .SingleAsync();
        Assert.Equal(RegistrationOutcome.ReRegistrationSuccess, evt.Outcome);
        Assert.Equal(existing.Id, evt.ResultingInstallationId);
    }

    [Fact]
    public async Task RegisterAsync_FreshTokenOnExistingActiveInstallation_TokenRaceLoserOnMarkUsed_RollsBackAndAuditsRaceOutcome()
    {
        // Same precondition as the happy path, but a fake
        // IBootstrapTokenService throws BootstrapTokenStateException on
        // MarkUsedAsync (the race-loser scenario). Re-registration must
        // roll back the in-flight revoke + new-credential insert and
        // write a failure audit with the race outcome.
        InstallationEntity existing = await SeedInstallationAsync(
            clientApp: "ButtonPanelTester", status: InstallationStatus.Active);
        InstallationApiCredentialEntity priorCred =
            await SeedActiveCredentialAsync(existing, "stak_prior-cred");
        const string plaintext = "stbt_race-loser";
        await SeedTokenAsync("ButtonPanelTester", plaintext);

        var realSvc = new BootstrapTokenService(new BootstrapTokenRepository(Context), _hasher,
            NullLogger<BootstrapTokenService>.Instance);
        var raceSvc = new RaceLosingBootstrapTokenService(realSvc, BootstrapTokenStatus.Used);
        RegistrationService sut = BuildSut(bootstrapSvcOverride: raceSvc);

        RegistrationResult result = await sut.RegisterAsync(BuildRequest(plaintext));

        RegistrationResult.Failure failure = Assert.IsType<RegistrationResult.Failure>(result);
        Assert.Equal(RegistrationOutcome.TokenAlreadyUsed, failure.Outcome);

        // Prior credential is still Active (revoke rolled back).
        InstallationApiCredentialEntity? priorAfter = await Context.InstallationApiCredentials
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == priorCred.Id);
        Assert.NotNull(priorAfter);
        Assert.Equal(InstallationStatus.Active, priorAfter!.Status);
        Assert.Null(priorAfter.RevokedAt);

        // No new credential was inserted.
        Assert.Equal(1, await Context.InstallationApiCredentials.CountAsync());

        // Audit row records the race outcome.
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking()
            .SingleAsync();
        Assert.Equal(RegistrationOutcome.TokenAlreadyUsed, evt.Outcome);
    }
}

internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;
    public FakeTimeProvider(DateTime utc) => _now = new DateTimeOffset(utc, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}

internal sealed class ThrowingRegistrationEventRepository : IRegistrationEventRepository
{
    public Task<RegistrationEventEntity> AddAsync(RegistrationEventEntity entity,
        CancellationToken ct = default)
        => throw new InvalidOperationException("audit-write failure (test-injected)");

    public Task<IReadOnlyList<RegistrationEventEntity>> ListBySourceAsync(string sourceIp,
        DateTime since, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RegistrationEventEntity>>([]);
}

/// <summary>
/// Decorator that delegates <see cref="LookupAsync"/> to a real
/// <see cref="IBootstrapTokenService"/> (so the registration flow validates
/// the token and reaches CommitSuccessAsync) and then synthesises the
/// race-loser failure mode by throwing
/// <see cref="BootstrapTokenStateException"/> from <see cref="MarkUsedAsync"/>.
/// </summary>
internal sealed class RaceLosingBootstrapTokenService : IBootstrapTokenService
{
    private readonly IBootstrapTokenService _inner;
    private readonly BootstrapTokenStatus _throwWith;

    public RaceLosingBootstrapTokenService(IBootstrapTokenService inner,
        BootstrapTokenStatus throwWith)
    {
        _inner = inner;
        _throwWith = throwWith;
    }

    public Task<BootstrapToken?> LookupAsync(string plaintext, CancellationToken ct = default)
        => _inner.LookupAsync(plaintext, ct);

    public Task<(BootstrapToken Record, string Plaintext)> MintAsync(string clientApp,
        TimeSpan? ttl, CancellationToken ct = default)
        => _inner.MintAsync(clientApp, ttl, ct);

    public Task MarkUsedAsync(int tokenId, int installationId, DateTime usedAt,
        CancellationToken ct = default)
        => throw new BootstrapTokenStateException(_throwWith,
            $"Race-loser test fake: token observed in {_throwWith} state.");
}
