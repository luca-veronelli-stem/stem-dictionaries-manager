using Core.Enums;
using Infrastructure.Entities;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Test per verificare che CreatedAt e UpdatedAt vengano settati automaticamente.
/// </summary>
public class AuditFieldsTests : IntegrationTestBase
{
    [Fact]
    public async Task Add_SetsCreatedAt()
    {
        var user = new UserEntity
        {
            Username = "testuser",
            DisplayName = "Test User"
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        Assert.NotEqual(default, user.CreatedAt);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public async Task Update_SetsUpdatedAt()
    {
        // Arrange - crea utente
        var user = new UserEntity
        {
            Username = "testuser",
            DisplayName = "Test User"
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        DateTime createdAt = user.CreatedAt;

        // Act - modifica utente
        await Task.Delay(10); // Assicura timestamp diverso
        user.DisplayName = "Updated Name";
        await Context.SaveChangesAsync();

        // Assert
        Assert.Equal(createdAt, user.CreatedAt); // CreatedAt non cambia
        Assert.NotNull(user.UpdatedAt);
        Assert.True(user.UpdatedAt > user.CreatedAt);
    }

    [Fact]
    public async Task AuditEntry_DoesNotHaveAuditFields()
    {
        // Prima crea un utente (necessario per FK)
        var user = new UserEntity
        {
            Username = "admin",
            DisplayName = "Admin"
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Crea AuditEntry
        var auditEntry = new AuditEntryEntity
        {
            EntityType = Core.Enums.AuditEntityType.User,
            EntityId = user.Id,
            Operation = Core.Enums.AuditOperation.Create,
            ChangedById = user.Id,
            ChangedAt = DateTime.UtcNow,
            NewValue = "{}"
        };

        Context.AuditEntries.Add(auditEntry);
        await Context.SaveChangesAsync();

        // AuditEntry non implementa IAuditable, quindi non ha CreatedAt/UpdatedAt
        Assert.True(auditEntry.Id > 0);
    }
}
