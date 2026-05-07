using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
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
        return new InstallationCredentialService(repo, generator, hasher);
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
}
