using Infrastructure;
using Infrastructure.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Tests.Unit.Infrastructure;

/// <summary>
/// Guards the EF model max length that the AppVersion-widening migration
/// mirrors: <see cref="InstallationEntity.AppVersion"/> and
/// <see cref="RegistrationEventEntity.ClaimedAppVersion"/> must stay at 128
/// so NBGV PR-build version strings (longer than the original 50) no longer
/// overflow the column and fail <c>POST /register</c>. These tests read the
/// configured EF model (the <c>HasMaxLength</c> metadata), not the database,
/// so they need no SQL Server connection.
/// </summary>
public class AppVersionColumnLengthTests
{
    private static AppDbContext NewContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Installation_AppVersion_MaxLengthIs128()
    {
        using AppDbContext ctx = NewContext();
        IProperty p = ctx.Model.FindEntityType(typeof(InstallationEntity))!
                  .FindProperty(nameof(InstallationEntity.AppVersion))!;
        Assert.Equal(128, p.GetMaxLength());
    }

    [Fact]
    public void RegistrationEvent_ClaimedAppVersion_MaxLengthIs128()
    {
        using AppDbContext ctx = NewContext();
        IProperty p = ctx.Model.FindEntityType(typeof(RegistrationEventEntity))!
                  .FindProperty(nameof(RegistrationEventEntity.ClaimedAppVersion))!;
        Assert.Equal(128, p.GetMaxLength());
    }
}
