using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Factory per creare AppDbContext durante le migrations (dotnet ef migrations add).
/// Usa SQL Server come provider target per produzione (Azure SQL).
/// Per sviluppo locale con SQLite, usare EnsureCreated o cambiare provider.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Connection string fittizia per generare migrations SQL Server.
    /// Non viene mai usata per connettersi: serve solo per indicare
    /// a EF Core quale provider SQL usare durante 'dotnet ef migrations add'.
    /// </summary>
    private const string DesignTimeConnectionString =
        "Server=placeholder;Database=placeholder;TrustServerCertificate=True;";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
