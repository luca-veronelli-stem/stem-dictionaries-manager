using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Factory per creare AppDbContext durante le migrations (dotnet ef migrations add).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Risale al solution root, poi naviga a Infrastructure/Data/
        var assemblyPath = Path.GetDirectoryName(typeof(DesignTimeDbContextFactory).Assembly.Location);
        var solutionPath = Path.GetFullPath(Path.Combine(assemblyPath!, "..", "..", "..", ".."));
        var dbPath = Path.Combine(solutionPath, "Infrastructure", "Data", "development.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }
}
