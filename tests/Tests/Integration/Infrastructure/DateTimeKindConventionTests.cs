using Core.Enums.Auth;
using Infrastructure;
using Infrastructure.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Asserts that every <c>DateTime</c> column round-trips through
/// <see cref="AppDbContext"/> with <see cref="DateTimeKind.Utc"/>.
/// Regression for the issue where SQLite (and SQL Server's
/// <c>datetime2</c>) returned <see cref="DateTimeKind.Unspecified"/>,
/// causing JSON serialization to drop the <c>Z</c> suffix and parsers
/// to treat the value as local time. Fixed by
/// <c>AppDbContext.ConfigureConventions</c>.
/// </summary>
public class DateTimeKindConventionTests : IntegrationTestBase
{
    [Fact]
    public async Task BootstrapTokenEntity_AllDateTimes_ReadBackAsUtc()
    {
        Context.BootstrapTokens.Add(new BootstrapTokenEntity
        {
            ClientApp = TestData.ClientApps.ButtonPanelTester,
            SecretHash = "hash-1",
            MintedAt = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 5, 8, 10, 0, 0, DateTimeKind.Utc),
            UsedAt = new DateTime(2026, 5, 7, 11, 0, 0, DateTimeKind.Utc),
            RevokedAt = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
            Status = BootstrapTokenStatus.Used
        });
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        BootstrapTokenEntity row = await Context.BootstrapTokens.AsNoTracking().SingleAsync();
        Assert.Equal(DateTimeKind.Utc, row.MintedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, row.ExpiresAt.Kind);
        Assert.Equal(DateTimeKind.Utc, row.UsedAt!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, row.RevokedAt!.Value.Kind);
    }

    [Fact]
    public async Task InstallationEntity_RegisteredAtAndRevokedAt_ReadBackAsUtc()
    {
        Context.Installations.Add(new InstallationEntity
        {
            ClientApp = TestData.ClientApps.ButtonPanelTester,
            OsUserId = "u",
            MachineId = "m",
            InstallGuid = Guid.NewGuid(),
            DescriptorJson = "{}",
            RegisteredAt = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
            RevokedAt = new DateTime(2026, 5, 8, 10, 0, 0, DateTimeKind.Utc),
            Status = InstallationStatus.Revoked
        });
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        InstallationEntity row = await Context.Installations.AsNoTracking().SingleAsync();
        Assert.Equal(DateTimeKind.Utc, row.RegisteredAt.Kind);
        Assert.Equal(DateTimeKind.Utc, row.RevokedAt!.Value.Kind);
    }

    [Fact]
    public async Task InstallationApiCredentialEntity_IssuedAtAndRevokedAt_ReadBackAsUtc()
    {
        Context.Installations.Add(new InstallationEntity
        {
            ClientApp = TestData.ClientApps.ButtonPanelTester,
            OsUserId = "u",
            MachineId = "m",
            InstallGuid = Guid.NewGuid(),
            DescriptorJson = "{}",
            RegisteredAt = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
            Status = InstallationStatus.Active
        });
        await Context.SaveChangesAsync();
        int installationId = (await Context.Installations.AsNoTracking().SingleAsync()).Id;

        Context.InstallationApiCredentials.Add(new InstallationApiCredentialEntity
        {
            InstallationId = installationId,
            SecretHash = "hash-2",
            IssuedAt = new DateTime(2026, 5, 7, 11, 0, 0, DateTimeKind.Utc),
            RevokedAt = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
            Status = InstallationStatus.Revoked
        });
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        InstallationApiCredentialEntity row = await Context.InstallationApiCredentials.AsNoTracking().SingleAsync();
        Assert.Equal(DateTimeKind.Utc, row.IssuedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, row.RevokedAt!.Value.Kind);
    }

    [Fact]
    public async Task RegistrationEventEntity_OccurredAt_ReadBackAsUtc()
    {
        Context.RegistrationEvents.Add(new RegistrationEventEntity
        {
            OccurredAt = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
            ClaimedClientApp = TestData.ClientApps.ButtonPanelTester,
            SourceIp = "127.0.0.1",
            Outcome = RegistrationOutcome.Success
        });
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        RegistrationEventEntity row = await Context.RegistrationEvents.AsNoTracking().SingleAsync();
        Assert.Equal(DateTimeKind.Utc, row.OccurredAt.Kind);
    }
}
