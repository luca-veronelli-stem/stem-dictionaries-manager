using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Factory per creare AppDbContext durante le migrations (dotnet ef migrations add).
/// Cerca la solution root risalendo la gerarchia delle directory fino a trovare il file .slnx.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var solutionPath = FindSolutionDirectory()
            ?? throw new InvalidOperationException(
                "Solution directory not found. Ensure you run 'dotnet ef' from within the solution directory structure.");

        var dbPath = Path.Combine(solutionPath, "Infrastructure", "Data", "development.db");

        // Crea la directory Data se non esiste
        var dataDir = Path.GetDirectoryName(dbPath)!;
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Cerca la directory della solution risalendo fino a trovare un file .slnx (o .sln per compatibilità).
    /// </summary>
    private static string? FindSolutionDirectory()
    {
        // Prova prima con la directory corrente (dove viene eseguito dotnet ef)
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory != null)
        {
            // Cerca .slnx (nuovo formato) o .sln (legacy)
            if (directory.GetFiles("*.slnx").Length > 0 || directory.GetFiles("*.sln").Length > 0)
                return directory.FullName;

            directory = directory.Parent;
        }

        // Fallback: prova dalla location dell'assembly (per compatibilità)
        var assemblyLocation = typeof(DesignTimeDbContextFactory).Assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)!);

            while (directory != null)
            {
                if (directory.GetFiles("*.slnx").Length > 0 || directory.GetFiles("*.sln").Length > 0)
                    return directory.FullName;

                directory = directory.Parent;
            }
        }

        return null;
    }
}
