using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Infrastructure.Repositories.Auth;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.Interfaces.Auth;

namespace Tests.Integration.Services.Auth;

public class RegistrationServiceTests : IntegrationTestBase
{
    private readonly PasswordHasher _hasher = new();
    private readonly TokenGenerator _generator = new();
    private readonly FakeTimeProvider _time = new(
        new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc));

    private RegistrationService BuildSut(IRegistrationEventRepository? eventsOverride = null,
        IBootstrapTokenService? bootstrapSvcOverride = null)
    {
        var bootstrapRepo = new BootstrapTokenRepository(Context);
        var installationRepo = new InstallationRepository(Context);
        var credentialRepo = new InstallationApiCredentialRepository(Context);
        IRegistrationEventRepository eventsRepo = eventsOverride
            ?? new RegistrationEventRepository(Context);

        IBootstrapTokenService bootstrapSvc = bootstrapSvcOverride
            ?? new BootstrapTokenService(bootstrapRepo, _hasher);
        var credentialSvc = new InstallationCredentialService(credentialRepo, _generator, _hasher);

        return new RegistrationService(Context, bootstrapSvc, credentialSvc,
            installationRepo, eventsRepo, _time);
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

    [Fact]
    public async Task RegisterAsync_DescriptorMissingMachineId_FailsWithDescriptorMalformed()
    {
        const string plaintext = "stbt_token-4";
        await SeedTokenAsync("ButtonPanelTester", plaintext);
        RegistrationService sut = BuildSut();

        RegisterRequest request = BuildRequest(plaintext) with { MachineId = "" };
        RegistrationResult result = await sut.RegisterAsync(request);

        Assert.IsType<RegistrationResult.Failure>(result);
        RegistrationEventEntity evt = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(RegistrationOutcome.DescriptorMalformed, evt.Outcome);
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

        BootstrapTokenService realSvc = new(new BootstrapTokenRepository(Context), _hasher);
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

        BootstrapTokenService realSvc = new(new BootstrapTokenRepository(Context), _hasher);
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
