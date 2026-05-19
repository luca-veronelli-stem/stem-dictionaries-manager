using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Repositories.Auth;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for <see cref="InstallationApiCredentialRepository"/>'s
/// re-registration lookup surface (#71 slice 1).
/// </summary>
public class InstallationApiCredentialRepositoryTests : IntegrationTestBase
{
    private readonly InstallationApiCredentialRepository _repository;

    public InstallationApiCredentialRepositoryTests()
    {
        _repository = new InstallationApiCredentialRepository(Context);
    }

    private async Task<InstallationEntity> SeedInstallationAsync()
    {
        InstallationEntity entity = new()
        {
            ClientApp = "ButtonPanelTester",
            OsUserId = "u1",
            MachineId = "m1",
            InstallGuid = Guid.NewGuid(),
            AppVersion = "1.0.0",
            DescriptorJson = "{}",
            RegisteredAt = DateTime.UtcNow,
            Status = InstallationStatus.Active
        };
        Context.Installations.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    private async Task<InstallationApiCredentialEntity> SeedCredentialAsync(
        InstallationEntity installation,
        InstallationStatus status, string secretHashSeed)
    {
        // Wire up the navigation explicitly. The entity declares
        // `Installation { get; set; } = null!;` (required FK), so adding a
        // credential without setting the navigation while the principal is
        // already tracked makes EF treat the relationship as severed.
        InstallationApiCredentialEntity entity = new()
        {
            InstallationId = installation.Id,
            Installation = installation,
            SecretHash = secretHashSeed,
            IssuedAt = DateTime.UtcNow,
            Status = status,
            RevokedAt = status == InstallationStatus.Revoked ? DateTime.UtcNow : null
        };
        Context.InstallationApiCredentials.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task ListActiveByInstallationIdAsync_OnlyActive_AreReturned()
    {
        InstallationEntity install = await SeedInstallationAsync();
        InstallationApiCredentialEntity activeCred = await SeedCredentialAsync(install,
            InstallationStatus.Active, secretHashSeed: "hash-active");
        await SeedCredentialAsync(install, InstallationStatus.Revoked,
            secretHashSeed: "hash-revoked");

        IReadOnlyList<InstallationApiCredentialEntity> hits = await _repository.ListActiveByInstallationIdAsync(install.Id);

        Assert.Single(hits);
        Assert.Equal(activeCred.Id, hits[0].Id);
        Assert.Equal(InstallationStatus.Active, hits[0].Status);
    }

    [Fact]
    public async Task ListActiveByInstallationIdAsync_NoMatches_ReturnsEmptyList()
    {
        InstallationEntity install = await SeedInstallationAsync();

        IReadOnlyList<InstallationApiCredentialEntity> hits = await _repository.ListActiveByInstallationIdAsync(install.Id);

        Assert.Empty(hits);
    }

    [Fact]
    public async Task ListActiveByInstallationIdAsync_DistinguishesInstallations()
    {
        InstallationEntity install1 = await SeedInstallationAsync();
        InstallationEntity install2 = await SeedInstallationAsync();
        await SeedCredentialAsync(install1, InstallationStatus.Active,
            secretHashSeed: "hash-1");
        await SeedCredentialAsync(install2, InstallationStatus.Active,
            secretHashSeed: "hash-2");

        IReadOnlyList<InstallationApiCredentialEntity> hits = await _repository.ListActiveByInstallationIdAsync(install1.Id);

        Assert.Single(hits);
        Assert.Equal(install1.Id, hits[0].InstallationId);
    }
}
