namespace Infrastructure;

/// <summary>
/// Transient one-shot migration of the SQLite database file from the legacy
/// Roaming layout (<c>%AppData%\STEM\DictionariesManager\</c>) to the APP_DATA
/// v1.9.0 layout (<c>%LocalAppData%\Stem\DictionariesManager\db\</c>), guarded
/// by a single-integer <c>.appdata-version</c> marker at the app root so it
/// runs at most once per installation.
/// </summary>
/// <remarks>
/// <para>
/// APP_DATA v1.9.0 conformance audit (#135): every per-user disk write in this
/// repo resolves under the conforming Local root
/// <c>%LocalAppData%\Stem\DictionariesManager\</c> via <see cref="StemAppData"/>.
/// The SQLite database is the repo's only on-disk per-user data, so there are
/// no <c>logs\</c>, <c>cache\</c>, or <c>credentials\</c> write sites to audit.
/// The legacy Roaming path built in <see cref="MigrateDefaultDatabase"/> is the
/// migration source only, never a write target.
/// </para>
/// <para>
/// Retain decision (#135): this transient helper is deliberately kept, not
/// removed yet. A production Azure deployment
/// (<c>app-dictionaries-manager-prod</c>) exists and the whole installed base
/// cannot be confirmed off the legacy Roaming root, so deleting the one-shot
/// migration now would risk stranding un-migrated users on the old path.
/// </para>
/// <para>
/// Removal target: the first release cycle after the next production rollover
/// confirms no first launch under the legacy Roaming layout can still surface.
/// At that point delete this class and its call site in
/// <see cref="DependencyInjection"/> (<c>GetDefaultSqlitePath</c>); the
/// <c>.appdata-version</c> files left on disk are inert and need no cleanup,
/// while <see cref="StemAppData"/> (the forever path-resolution helper) stays.
/// See <c>docs/Standards/APP_DATA.md</c> -> "Removal after cutover".
/// </para>
/// </remarks>
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
