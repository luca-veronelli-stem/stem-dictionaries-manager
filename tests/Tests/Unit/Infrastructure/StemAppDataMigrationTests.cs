using Infrastructure;

namespace Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for the transient one-shot SQLite database migration from the
/// legacy Roaming path to the APP_DATA v1.9.0 <c>db\</c> sub-folder.
/// </summary>
public class StemAppDataMigrationTests
{
    [Fact]
    public void MigrateOnce_ExistingLegacyDatabase_MovesItOnceThenNoOp()
    {
        string sandbox = Path.Combine(
            Path.GetTempPath(), "stem-migrate-" + Guid.NewGuid().ToString("N"));
        string legacyDir = Path.Combine(sandbox, "legacy");
        string appRoot = Path.Combine(sandbox, "Stem", "DictionariesManager");
        string newDbDir = Path.Combine(appRoot, "db");
        Directory.CreateDirectory(legacyDir);
        string legacyDb = Path.Combine(legacyDir, "sqldb-dictionaries-manager-test.db");
        File.WriteAllText(legacyDb, "original-db-bytes");

        try
        {
            // First call: the legacy file is moved into db\ and reported as moved.
            bool firstMoved = StemAppDataMigration.MigrateOnce(legacyDb, newDbDir, appRoot);
            string movedDb = Path.Combine(newDbDir, "sqldb-dictionaries-manager-test.db");

            Assert.True(firstMoved);
            Assert.False(File.Exists(legacyDb));
            Assert.True(File.Exists(movedDb));
            Assert.Equal("original-db-bytes", File.ReadAllText(movedDb));
            Assert.Equal("1", File.ReadAllText(Path.Combine(appRoot, ".appdata-version")));

            // A stale file reappears at the legacy path; the marker makes the
            // second call a no-op (does not move, does not overwrite).
            File.WriteAllText(legacyDb, "stale-bytes");
            bool secondMoved = StemAppDataMigration.MigrateOnce(legacyDb, newDbDir, appRoot);

            Assert.False(secondMoved);
            Assert.True(File.Exists(legacyDb));
            Assert.Equal("original-db-bytes", File.ReadAllText(movedDb));
        }
        finally
        {
            Directory.Delete(sandbox, recursive: true);
        }
    }
}
