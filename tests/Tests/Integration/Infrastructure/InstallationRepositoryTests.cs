using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Repositories.Auth;
using Tests.Shared;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for <see cref="InstallationRepository"/>'s
/// re-registration lookup surface (#71 slice 1).
/// </summary>
public class InstallationRepositoryTests : IntegrationTestBase
{
    private readonly InstallationRepository _repository;

    public InstallationRepositoryTests()
    {
        _repository = new InstallationRepository(Context);
    }

    private async Task<InstallationEntity> SeedInstallationAsync(Guid installGuid,
        string clientApp = TestData.ClientApps.ButtonPanelTester,
        InstallationStatus status = InstallationStatus.Active)
    {
        InstallationEntity entity = new()
        {
            ClientApp = clientApp,
            OsUserId = "u1",
            MachineId = "m1",
            InstallGuid = installGuid,
            AppVersion = "1.0.0",
            DescriptorJson = "{}",
            RegisteredAt = DateTime.UtcNow,
            Status = status
        };
        Context.Installations.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task FindByInstallGuidAsync_WhenRowExists_ReturnsRow()
    {
        var g = Guid.NewGuid();
        InstallationEntity seeded = await SeedInstallationAsync(g);

        InstallationEntity? hit = await _repository.FindByInstallGuidAsync(g);

        Assert.NotNull(hit);
        Assert.Equal(seeded.Id, hit.Id);
        Assert.Equal(g, hit.InstallGuid);
    }

    [Fact]
    public async Task FindByInstallGuidAsync_WhenNoRowExists_ReturnsNull()
    {
        InstallationEntity? hit = await _repository.FindByInstallGuidAsync(Guid.NewGuid());

        Assert.Null(hit);
    }

    [Fact]
    public async Task FindByInstallGuidAsync_DistinguishesGuids_DoesNotReturnUnrelatedRow()
    {
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        await SeedInstallationAsync(g1);

        InstallationEntity? hit = await _repository.FindByInstallGuidAsync(g2);

        Assert.Null(hit);
    }
}
