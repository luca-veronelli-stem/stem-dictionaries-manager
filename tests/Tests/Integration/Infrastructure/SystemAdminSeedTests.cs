using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Proves the system-admin user is seeded on both provider paths: SQLite via
/// EnsureCreated (which honors only HasData) and, by carrying the same seed in
/// the model, SQL Server via the existing AddBootstrapRegistration migration.
/// </summary>
public class SystemAdminSeedTests : IntegrationTestBase
{
    [Fact]
    public async Task EnsureCreated_SeedsSystemAdminUser()
    {
        // IntegrationTestBase builds a fresh SQLite DB via EnsureCreated, which
        // applies HasData but not migration InsertData. The system-admin row
        // must now be present (previously it was missing on SQLite, silently
        // breaking AdminAuthenticationMiddleware audit attribution).
        UserEntity? admin = await Context.Users
            .FirstOrDefaultAsync(u => u.Username == "system-admin");

        Assert.NotNull(admin);
        Assert.Equal(1, admin.Id);
        Assert.Equal("System Admin (API key)", admin.DisplayName);
    }

    [Fact]
    public void Model_CarriesSystemAdminSeed()
    {
        // The seed lives in the model (HasData), so the SQL Server migrate path
        // stays consistent: the seeded row is identical (Id 1, same values) to
        // the one AddBootstrapRegistration's InsertData already produced.
        IModel model = Context.GetService<IDesignTimeModel>().Model;
        IEntityType userType = model.FindEntityType(typeof(UserEntity))!;
        IDictionary<string, object?> seed = userType.GetSeedData().Single();

        Assert.Equal(1, (int)seed["Id"]!);
        Assert.Equal("system-admin", (string)seed["Username"]!);
    }
}
