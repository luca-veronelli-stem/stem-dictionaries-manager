using Infrastructure;

namespace Tests.Integration.API;

/// <summary>
/// Guards that API-host integration tests never resolve the default
/// <c>%LocalAppData%\Stem\DictionariesManager</c> SQLite path. Resolving it
/// runs <see cref="StemAppDataMigration.MigrateOnce"/> against the
/// machine-global <c>.appdata-version</c> marker, which races other parallel
/// <c>WebApplicationFactory</c> hosts on a cold runner (#113).
/// </summary>
public class ApiHostAppDataIsolationTests
{
    [Fact]
    public void ResolveConnectionString_ForApiHost_AvoidsMachineGlobalAppData()
    {
        // The test project pre-sets ConnectionStrings__Sqlite (the seam
        // Program.cs reads at WebApplication.CreateBuilder time) to a
        // throwaway path, so connection-string resolution never takes the
        // default-path branch that touches the real user profile.
        string? configured = Environment.GetEnvironmentVariable("ConnectionStrings__Sqlite");
        Assert.False(string.IsNullOrWhiteSpace(configured),
            "API integration tests must pre-set ConnectionStrings__Sqlite to an isolated path");

        string resolved = DependencyInjection.ResolveConnectionString(configured, isSqlServer: false);

        string stemRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Stem", "DictionariesManager");
        Assert.DoesNotContain(stemRoot, resolved, StringComparison.OrdinalIgnoreCase);
    }
}
