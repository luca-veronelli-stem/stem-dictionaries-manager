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
    /// Safe to call on every startup, safe to call twice, and safe to call from
    /// two processes at once: marker I/O shares the file and tolerates a
    /// concurrent writer, so racing startups no-op instead of crashing.
    /// </summary>
    public static bool MigrateOnce(string legacyDbPath, string newDbDir, string appRoot)
    {
        string marker = Path.Combine(appRoot, MarkerFileName);
        if (ReadMarkerVersion(marker) >= SchemaVersion)
        {
            return false;
        }

        bool moved = MoveLegacyDatabase(legacyDbPath, newDbDir);

        Directory.CreateDirectory(appRoot);
        TryWriteMarker(marker);
        return moved;
    }

    /// <summary>
    /// Reads the marker's recorded schema version, sharing the file with other
    /// handles. A missing, unparsable, or concurrently-held marker counts as 0
    /// (not yet migrated) so a contended startup retries idempotently rather
    /// than throwing.
    /// </summary>
    private static int ReadMarkerVersion(string marker)
    {
        if (!File.Exists(marker))
        {
            return 0;
        }
        try
        {
            using var stream = new FileStream(
                marker, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return int.TryParse(reader.ReadToEnd(), out int v) ? v : 0;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Moves the legacy database into the new folder when present and not
    /// already there. A concurrent process that moved it first surfaces as an
    /// <see cref="IOException"/>, which is treated as already-migrated.
    /// </summary>
    private static bool MoveLegacyDatabase(string legacyDbPath, string newDbDir)
    {
        string target = Path.Combine(newDbDir, Path.GetFileName(legacyDbPath));
        if (!File.Exists(legacyDbPath) || File.Exists(target))
        {
            return false;
        }
        Directory.CreateDirectory(newDbDir);
        try
        {
            File.Move(legacyDbPath, target);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Records the completed schema version, sharing the file and tolerating a
    /// concurrent writer: if another process is recording the same marker the
    /// migration is still effectively done, so a sharing
    /// <see cref="IOException"/> is a no-op rather than a crash.
    /// </summary>
    private static void TryWriteMarker(string marker)
    {
        try
        {
            using var stream = new FileStream(
                marker, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.Write(SchemaVersion.ToString());
        }
        catch (IOException)
        {
            // Another process is recording the same marker; nothing more to do.
        }
    }
}
