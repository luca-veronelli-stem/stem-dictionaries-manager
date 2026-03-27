namespace Infrastructure;

/// <summary>
/// Popola il database con dati iniziali.
/// Attualmente vuoto: i dati vengono inseriti manualmente dalla GUI.
/// </summary>
public static class DatabaseSeeder
{
    public static Task SeedAsync(AppDbContext context)
    {
        // Nessun seeding automatico - dati inseriti manualmente dalla GUI
        return Task.CompletedTask;
    }
}
