using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Services.Auth;
using Tests.Unit.Services.Auth.Fakes;

namespace Tests.Unit.Services.Auth;

public class InstallationCredentialServiceTests
{
    private static readonly DateTime _issuedAt = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);

    private static InstallationCredentialService BuildSut(
        out FakeInstallationApiCredentialRepository repo,
        out FakeTokenGenerator generator,
        out FakePasswordHasher hasher)
    {
        repo = new FakeInstallationApiCredentialRepository();
        generator = new FakeTokenGenerator();
        hasher = new FakePasswordHasher();
        return new InstallationCredentialService(repo, generator, hasher, NullLogger<InstallationCredentialService>.Instance);
    }

    [Fact]
    public async Task IssueAsync_ReturnsPlaintextAndPersistsHashOnly()
    {
        InstallationCredentialService sut = BuildSut(out FakeInstallationApiCredentialRepository repo,
            out _, out _);

        (InstallationApiCredential record, string plaintext) =
            await sut.IssueAsync(installationId: 142, issuedAt: _issuedAt);

        Assert.StartsWith("stak_", plaintext);
        Assert.Equal(142, record.InstallationId);
        Assert.Equal(_issuedAt, record.IssuedAt);
        Assert.Equal(InstallationStatus.Active, record.Status);
        // Hashed-not-plaintext — invariant 4.
        Assert.NotEqual(plaintext, record.SecretHash);
        Assert.Contains(plaintext, record.SecretHash);

        // Row was added (active) — exactly one matching credential.
        IReadOnlyList<InstallationApiCredentialEntity> active = await repo.ListAllActiveAsync();
        Assert.Single(active);
        Assert.Equal(142, active[0].InstallationId);
        Assert.Equal(record.SecretHash, active[0].SecretHash);
    }

    [Fact]
    public async Task IssueAsync_TwoCallsReturnDistinctPlaintexts()
    {
        InstallationCredentialService sut = BuildSut(out _, out _, out _);

        (_, string a) = await sut.IssueAsync(installationId: 1, issuedAt: _issuedAt);
        (_, string b) = await sut.IssueAsync(installationId: 2, issuedAt: _issuedAt);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task IssueAsync_NonPositiveInstallationId_Throws()
    {
        InstallationCredentialService sut = BuildSut(out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await sut.IssueAsync(installationId: 0, issuedAt: _issuedAt));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await sut.IssueAsync(installationId: -1, issuedAt: _issuedAt));
    }

    private static InstallationApiCredentialEntity SeedCredential(
        FakeInstallationApiCredentialRepository repo, int installationId,
        InstallationStatus status, string secretHash)
    {
        InstallationApiCredentialEntity entity = new()
        {
            InstallationId = installationId,
            SecretHash = secretHash,
            IssuedAt = _issuedAt,
            Status = status,
            RevokedAt = status == InstallationStatus.Revoked ? _issuedAt : null
        };
        repo.Seed(entity);
        return entity;
    }

    [Fact]
    public async Task RevokeActiveAsync_NoActiveCredentials_ReturnsZero()
    {
        InstallationCredentialService sut = BuildSut(
            out FakeInstallationApiCredentialRepository repo, out _, out _);

        int flipped = await sut.RevokeActiveAsync(installationId: 142,
            revokedAt: _issuedAt);

        Assert.Equal(0, flipped);
        Assert.Empty(await repo.ListAllActiveAsync());
    }

    [Fact]
    public async Task RevokeActiveAsync_OneActive_FlipsStatusAndRevokedAt_ReturnsOne()
    {
        InstallationCredentialService sut = BuildSut(
            out FakeInstallationApiCredentialRepository repo, out _, out _);
        InstallationApiCredentialEntity seeded = SeedCredential(repo,
            installationId: 142, status: InstallationStatus.Active,
            secretHash: "hash-active");

        DateTime revokedAt = _issuedAt.AddMinutes(15);
        int flipped = await sut.RevokeActiveAsync(installationId: 142,
            revokedAt: revokedAt);

        Assert.Equal(1, flipped);
        Assert.Equal(InstallationStatus.Revoked, seeded.Status);
        Assert.Equal(revokedAt, seeded.RevokedAt);
        Assert.Empty(await repo.ListAllActiveAsync());
    }

    [Fact]
    public async Task RevokeActiveAsync_MixedActiveAndRevoked_OnlyFlipsActive_ReturnsActiveCount()
    {
        InstallationCredentialService sut = BuildSut(
            out FakeInstallationApiCredentialRepository repo, out _, out _);
        InstallationApiCredentialEntity active = SeedCredential(repo,
            installationId: 142, status: InstallationStatus.Active,
            secretHash: "hash-active");
        InstallationApiCredentialEntity revoked = SeedCredential(repo,
            installationId: 142, status: InstallationStatus.Revoked,
            secretHash: "hash-revoked");
        DateTime priorRevokedAt = revoked.RevokedAt!.Value;

        DateTime revokedAt = _issuedAt.AddMinutes(15);
        int flipped = await sut.RevokeActiveAsync(installationId: 142,
            revokedAt: revokedAt);

        Assert.Equal(1, flipped);
        Assert.Equal(InstallationStatus.Revoked, active.Status);
        Assert.Equal(revokedAt, active.RevokedAt);
        // Pre-existing Revoked row's RevokedAt is preserved.
        Assert.Equal(priorRevokedAt, revoked.RevokedAt);
    }

    [Fact]
    public async Task RevokeActiveAsync_DistinguishesInstallations()
    {
        InstallationCredentialService sut = BuildSut(
            out FakeInstallationApiCredentialRepository repo, out _, out _);
        InstallationApiCredentialEntity left = SeedCredential(repo,
            installationId: 142, status: InstallationStatus.Active,
            secretHash: "hash-1");
        InstallationApiCredentialEntity right = SeedCredential(repo,
            installationId: 200, status: InstallationStatus.Active,
            secretHash: "hash-2");

        int flipped = await sut.RevokeActiveAsync(installationId: 142,
            revokedAt: _issuedAt);

        Assert.Equal(1, flipped);
        Assert.Equal(InstallationStatus.Revoked, left.Status);
        Assert.Equal(InstallationStatus.Active, right.Status);
    }

    [Fact]
    public async Task RevokeActiveAsync_NonPositiveInstallationId_Throws()
    {
        InstallationCredentialService sut = BuildSut(out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await sut.RevokeActiveAsync(installationId: 0,
                revokedAt: _issuedAt));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await sut.RevokeActiveAsync(installationId: -1,
                revokedAt: _issuedAt));
    }
}
