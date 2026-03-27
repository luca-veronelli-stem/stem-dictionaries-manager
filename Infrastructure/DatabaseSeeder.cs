using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Popola il database con dati iniziali.
/// Solo utenti: il resto viene inserito manualmente dalla GUI.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Se esistono già utenti, non fare nulla
        if (await context.Users.AnyAsync())
            return;

        // Utenti del team firmware STEM
        var users = new[]
        {
            new UserEntity { Username = "luca.veronelli", DisplayName = "Luca Veronelli" },
            new UserEntity { Username = "alessandro.goldoni", DisplayName = "Alessandro Goldoni" },
            new UserEntity { Username = "andrea.acunzo", DisplayName = "Andrea Acunzo" },
            new UserEntity { Username = "michele.pignedoli", DisplayName = "Michele Pignedoli" },
            new UserEntity { Username = "lorenzo.vecchi", DisplayName = "Lorenzo Vecchi" }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}
