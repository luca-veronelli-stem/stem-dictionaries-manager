namespace Infrastructure;

/// <summary>
/// Transient one-shot migration of the SQLite database file from the legacy
/// Roaming layout (<c>%AppData%\STEM\DictionariesManager\</c>) to the APP_DATA
/// v1.9.0 layout (<c>%LocalAppData%\Stem\DictionariesManager\db\</c>), guarded
/// by a single-integer <c>.appdata-version</c> marker at the app root so it
/// runs at most once per installation.
///
/// Delete this class and its call site in <see cref="DependencyInjection"/>
/// once the installed base (devs + CI) has rolled over to the new path.
/// </summary>
public static class StemAppDataMigration
{
    /// <summary>On-disk schema version recorded by a completed migration.</summary>
    public const int SchemaVersion = 1;

    private const string MarkerFileName = ".appdata-version";

    /// <summary>
    /// Production entry point: move the default SQLite database from the legacy
    /// Roaming path into the new <c>db\</c> folder, once.
    /// </summary>
    public static void MigrateDefaultDatabase(string databaseFileName)
    {
        string legacyDbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "STEM", "DictionariesManager", databaseFileName);
        MigrateOnce(legacyDbPath, StemAppData.GetDbDir(), StemAppData.GetAppRoot());
    }

    /// <summary>
    /// Idempotent core: move <paramref name="legacyDbPath"/> into
    /// <paramref name="newDbDir"/> unless the marker at <paramref name="appRoot"/>
    /// already records the migration. Returns <c>true</c> if a file was moved.
    /// Safe to call on every startup and safe to call twice.
    /// </summary>
    public static bool MigrateOnce(string legacyDbPath, string newDbDir, string appRoot)
    {
        string marker = Path.Combine(appRoot, MarkerFileName);
        int current = File.Exists(marker) && int.TryParse(File.ReadAllText(marker), out int v)
            ? v
            : 0;
        if (current >= SchemaVersion)
        {
            return false;
        }

        bool moved = false;
        string target = Path.Combine(newDbDir, Path.GetFileName(legacyDbPath));
        if (File.Exists(legacyDbPath) && !File.Exists(target))
        {
            Directory.CreateDirectory(newDbDir);
            File.Move(legacyDbPath, target);
            moved = true;
        }

        Directory.CreateDirectory(appRoot);
        File.WriteAllText(marker, SchemaVersion.ToString());
        return moved;
    }
}
