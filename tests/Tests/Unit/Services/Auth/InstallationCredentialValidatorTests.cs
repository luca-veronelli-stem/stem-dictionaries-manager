using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Services.Auth;
using Tests.Unit.Services.Auth.Fakes;

namespace Tests.Unit.Services.Auth;

public class InstallationCredentialValidatorTests
{
    private readonly FakeMemoryCache _cache = new();
    private readonly FakeInstallationApiCredentialRepository _credentials = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly InstallationCredentialValidator _sut;

    public InstallationCredentialValidatorTests()
    {
        _sut = new InstallationCredentialValidator(_cache, _credentials, _hasher);
    }

    private void SeedActiveCredential(int installationId, string plaintext)
    {
        _credentials.Seed(new InstallationApiCredentialEntity
        {
            Id = installationId,
            InstallationId = installationId,
            SecretHash = _hasher.Hash(plaintext),
            IssuedAt = DateTime.UtcNow,
            Status = InstallationStatus.Active
        });
    }

    [Fact]
    public async Task ValidateAsync_KnownPlaintext_ReturnsInstallationId()
    {
        SeedActiveCredential(installationId: 42, plaintext: "stak_known-active-plaintext");

        int? result = await _sut.ValidateAsync("stak_known-active-plaintext");

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ValidateAsync_UnknownPlaintext_ReturnsNull()
    {
        SeedActiveCredential(installationId: 1, plaintext: "stak_only-this-is-known");

        int? result = await _sut.ValidateAsync("stak_definitely-not-known");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_CalledTwiceWithSamePlaintext_RepoQueriedOnlyOnce()
    {
        // R4: cache hit short-circuits the (expensive) PBKDF2 verification.
        SeedActiveCredential(installationId: 7, plaintext: "stak_cache-hit-test");

        await _sut.ValidateAsync("stak_cache-hit-test");
        await _sut.ValidateAsync("stak_cache-hit-test");

        Assert.Equal(1, _credentials.ListAllActiveAsyncCallCount);
    }

    [Fact]
    public async Task ValidateAsync_CachesNegativeResultsToo()
    {
        // Negative caching is required: an attacker that submits the same
        // unknown plaintext repeatedly must not pin the iteration cost on
        // the request path (DoS hedge per R4).
        SeedActiveCredential(installationId: 1, plaintext: "stak_real");

        await _sut.ValidateAsync("stak_unknown");
        await _sut.ValidateAsync("stak_unknown");

        Assert.Equal(1, _credentials.ListAllActiveAsyncCallCount);
    }

    [Fact]
    public async Task Invalidate_AfterCachedHit_NextValidateAsyncQueriesRepoAgain()
    {
        // SC-004: explicit invalidation is the steady-state path for the
        // single-instance API host. Revoke flow calls Invalidate after the
        // DB write commits.
        SeedActiveCredential(installationId: 11, plaintext: "stak_revoke-target");
        await _sut.ValidateAsync("stak_revoke-target");
        Assert.Equal(1, _credentials.ListAllActiveAsyncCallCount);

        _sut.Invalidate("stak_revoke-target");

        await _sut.ValidateAsync("stak_revoke-target");
        Assert.Equal(2, _credentials.ListAllActiveAsyncCallCount);
    }

    [Fact]
    public async Task ValidateAsync_AfterTtlExpiry_QueriesRepoAgain()
    {
        // SC-004 / R4: 5-second absolute TTL ceiling. Validates that the
        // entry is configured with the documented 5 s window — the fake
        // clock advance lets us assert without sleeping in test time.
        SeedActiveCredential(installationId: 5, plaintext: "stak_ttl-test");
        await _sut.ValidateAsync("stak_ttl-test");
        Assert.Equal(1, _credentials.ListAllActiveAsyncCallCount);

        _cache.Advance(InstallationCredentialValidator.CacheTtl + TimeSpan.FromMilliseconds(1));

        await _sut.ValidateAsync("stak_ttl-test");
        Assert.Equal(2, _credentials.ListAllActiveAsyncCallCount);
    }

    [Fact]
    public async Task ValidateAsync_BeforeTtlExpiry_DoesNotRequeryRepo()
    {
        SeedActiveCredential(installationId: 5, plaintext: "stak_within-ttl");
        await _sut.ValidateAsync("stak_within-ttl");

        _cache.Advance(InstallationCredentialValidator.CacheTtl - TimeSpan.FromMilliseconds(1));

        await _sut.ValidateAsync("stak_within-ttl");
        Assert.Equal(1, _credentials.ListAllActiveAsyncCallCount);
    }

    [Fact]
    public async Task ValidateAsync_KeyIsHashed_PlaintextNeverAppearsInCache()
    {
        // R4: cache key is Sha256(plaintext), not the plaintext itself —
        // a memory dump must not leak the secret beyond the immediate
        // request scope.
        const string plaintext = "stak_secret-must-not-leak-to-cache-keys-99";
        SeedActiveCredential(installationId: 99, plaintext: plaintext);

        await _sut.ValidateAsync(plaintext);

        Assert.DoesNotContain(_cache.Keys, k => k is string s && s.Contains(plaintext));
    }

    [Fact]
    public async Task ValidateAsync_RevokedCredential_ReturnsNull()
    {
        const string plaintext = "stak_was-active-now-revoked";
        _credentials.Seed(new InstallationApiCredentialEntity
        {
            Id = 1,
            InstallationId = 1,
            SecretHash = _hasher.Hash(plaintext),
            IssuedAt = DateTime.UtcNow,
            Status = InstallationStatus.Revoked,
            RevokedAt = DateTime.UtcNow
        });

        int? result = await _sut.ValidateAsync(plaintext);

        Assert.Null(result);
    }
}
