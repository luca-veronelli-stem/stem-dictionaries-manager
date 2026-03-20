namespace Tests.Integration.Infrastructure;

/// <summary>
/// Test per verificare che il DB venga creato correttamente.
/// </summary>
public class DatabaseCreationTests : IntegrationTestBase
{
    [Fact]
    public void Database_CanBeCreated()
    {
        // Se arriviamo qui senza eccezioni, il DB è stato creato
        Assert.True(Context.Database.CanConnect());
    }

    [Fact]
    public void AllDbSets_AreAccessible()
    {
        Assert.NotNull(Context.Users);
        Assert.NotNull(Context.BoardTypes);
        Assert.NotNull(Context.Boards);
        Assert.NotNull(Context.Variables);
        Assert.NotNull(Context.Dictionaries);
        Assert.NotNull(Context.BitInterpretations);
        Assert.NotNull(Context.Commands);
        Assert.NotNull(Context.CommandDeviceStates);
        Assert.NotNull(Context.AuditEntries);
    }
}
