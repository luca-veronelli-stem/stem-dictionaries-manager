using Infrastructure;

namespace Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for the APP_DATA v1.9.0 path resolution
/// (<c>%LocalAppData%\Stem\DictionariesManager\db\</c>).
/// </summary>
public class StemAppDataTests
{
    [Fact]
    public void BuildDbDir_ProducesStemPascalCaseDbSubfolder()
    {
        string localRoot = Path.Combine(Path.GetTempPath(), "local-root-probe");

        string dbDir = StemAppData.BuildDbDir(localRoot);

        Assert.Equal(
            Path.Combine(localRoot, "Stem", "DictionariesManager", "db"),
            dbDir);
    }

    [Fact]
    public void GetDbDir_ResolvesUnderLocalApplicationData()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string dbDir = StemAppData.GetDbDir();

        Assert.StartsWith(
            Path.Combine(local, "Stem", "DictionariesManager", "db"),
            dbDir);
    }
}
