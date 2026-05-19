using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for the filtered unique index that enforces
/// "at most one Active <see cref="InstallationApiCredentialEntity"/>
/// per Installation" (spec 002, data-model invariant 6, #71 slice 2).
/// </summary>
public class InstallationApiCredentialUniqueActiveTests : IntegrationTestBase
{
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

    private static InstallationApiCredentialEntity Credential(
        InstallationEntity install, InstallationStatus status, string secretHash)
        => new()
        {
            InstallationId = install.Id,
            Installation = install,
            SecretHash = secretHash,
            IssuedAt = DateTime.UtcNow,
            Status = status,
            RevokedAt = status == InstallationStatus.Revoked ? DateTime.UtcNow : null
        };

    [Fact]
    public async Task Insert_SecondActiveCredentialForSameInstallation_ThrowsUniqueConstraintViolation()
    {
        InstallationEntity install = await SeedInstallationAsync();
        Context.InstallationApiCredentials.Add(Credential(install,
            InstallationStatus.Active, secretHash: "hash-1"));
        await Context.SaveChangesAsync();

        Context.InstallationApiCredentials.Add(Credential(install,
            InstallationStatus.Active, secretHash: "hash-2"));

        await Assert.ThrowsAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Insert_OneActiveAndOneRevoked_ForSameInstallation_Succeeds()
    {
        InstallationEntity install = await SeedInstallationAsync();
        Context.InstallationApiCredentials.Add(Credential(install,
            InstallationStatus.Active, secretHash: "hash-active"));
        Context.InstallationApiCredentials.Add(Credential(install,
            InstallationStatus.Revoked, secretHash: "hash-revoked"));

        await Context.SaveChangesAsync();

        Assert.Equal(2, await Context.InstallationApiCredentials
            .CountAsync(c => c.InstallationId == install.Id));
    }

    [Fact]
    public async Task Insert_TwoActive_ForDifferentInstallations_Succeeds()
    {
        InstallationEntity install1 = await SeedInstallationAsync();
        InstallationEntity install2 = await SeedInstallationAsync();
        Context.InstallationApiCredentials.Add(Credential(install1,
            InstallationStatus.Active, secretHash: "hash-1"));
        Context.InstallationApiCredentials.Add(Credential(install2,
            InstallationStatus.Active, secretHash: "hash-2"));

        await Context.SaveChangesAsync();

        Assert.Equal(2, await Context.InstallationApiCredentials.CountAsync());
    }
}
