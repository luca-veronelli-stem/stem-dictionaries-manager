namespace Infrastructure;

/// <summary>
/// Resolves the per-user data directories for DictionariesManager under the
/// STEM APP_DATA v1.9.0 root: <c>%LocalAppData%\Stem\DictionariesManager\</c>
/// (PascalCase <c>Stem</c> company segment, <c>LocalApplicationData</c> not
/// Roaming). SQLite databases live under the <c>db\</c> sub-folder.
/// </summary>
public static class StemAppData
{
    private const string CompanySegment = "Stem";
    private const string AppSegment = "DictionariesManager";

    /// <summary>
    /// Per-app root <c>&lt;LocalApplicationData&gt;\Stem\DictionariesManager</c>,
    /// created if missing.
    /// </summary>
    public static string GetAppRoot() => EnsureDir(BuildAppRoot(LocalRoot));

    /// <summary>Database sub-folder <c>&lt;AppRoot&gt;\db</c>, created if missing.</summary>
    public static string GetDbDir() => EnsureDir(BuildDbDir(LocalRoot));

    /// <summary>
    /// Pure (filesystem-free) builder for the per-app root under an arbitrary
    /// local-data root. Used by the production resolvers above and by tests.
    /// </summary>
    public static string BuildAppRoot(string localRoot) =>
        Path.Combine(localRoot, CompanySegment, AppSegment);

    /// <summary>Pure (filesystem-free) builder for the <c>db\</c> sub-folder.</summary>
    public static string BuildDbDir(string localRoot) =>
        Path.Combine(BuildAppRoot(localRoot), "db");

    private static string LocalRoot =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
