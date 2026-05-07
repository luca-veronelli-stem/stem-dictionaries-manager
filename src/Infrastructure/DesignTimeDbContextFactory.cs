using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Factory that creates AppDbContext during migrations (dotnet ef migrations add).
/// Uses SQL Server as the target provider for production (Azure SQL).
/// For local development with SQLite, use EnsureCreated or switch the provider.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Dummy connection string used to generate SQL Server migrations.
    /// It is never used to connect: it only tells EF Core which SQL provider
    /// to target during 'dotnet ef migrations add'.
    /// </summary>
    private const string DesignTimeConnectionString =
        "Server=placeholder;Database=placeholder;TrustServerCertificate=True;";

    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
