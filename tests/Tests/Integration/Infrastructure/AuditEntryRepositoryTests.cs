using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per AuditEntryRepository.
/// Verifica che AuditEntry non possa essere modificato o cancellato.
/// </summary>
public class AuditEntryRepositoryTests : IntegrationTestBase
{
    private readonly AuditEntryRepository _repository;
    private UserEntity _testUser = null!;

    public AuditEntryRepositoryTests()
    {
        _repository = new AuditEntryRepository(Context);
    }

    public override async Task InitializeAsync()
    {
        _testUser = new UserEntity { Username = "admin", DisplayName = "Admin" };
        Context.Users.Add(_testUser);
        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_CreatesAuditEntry()
    {
        var entry = new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow,
            NewValue = "{\"name\":\"test\"}"
        };

        var result = await _repository.AddAsync(entry);

        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsInvalidOperationException()
    {
        var entry = new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entry);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.UpdateAsync(entry));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsInvalidOperationException()
    {
        var entry = new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entry);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.DeleteAsync(entry.Id));
    }

    [Fact]
    public async Task GetByEntityAsync_ReturnsMatchingEntries()
    {
        // Arrange
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 42,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow.AddMinutes(-2)
        });
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 42,
            Operation = AuditOperation.Update,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Command, // Diverso
            EntityId = 42,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow
        });

        // Act
        var result = await _repository.GetByEntityAsync(AuditEntityType.Variable, 42);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(AuditEntityType.Variable, e.EntityType));
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsOrderedByDate()
    {
        // Arrange
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow.AddHours(-2)
        });
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 2,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow.AddHours(-1)
        });
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 3,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = DateTime.UtcNow
        });

        // Act
        var result = await _repository.GetRecentAsync(2);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].EntityId); // Più recente prima
        Assert.Equal(2, result[1].EntityId);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsEntriesInRange()
    {
        // Arrange
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            NewValue = "{}"
        });
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 2,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            NewValue = "{}"
        });
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 3,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = new DateTime(2026, 12, 31, 23, 0, 0, DateTimeKind.Utc),
            NewValue = "{}"
        });

        // Act
        var result = await _repository.GetByDateRangeAsync(
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].EntityId);
    }

    [Fact]
    public async Task GetByDateRangeAsync_IncludesBoundaryDates()
    {
        // Arrange
        var exactDate = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = exactDate,
            NewValue = "{}"
        });

        // Act — from e to coincidono con ChangedAt
        var result = await _repository.GetByDateRangeAsync(exactDate, exactDate);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetByDateRangeAsync_NoMatches_ReturnsEmpty()
    {
        // Arrange
        await _repository.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            NewValue = "{}"
        });

        // Act
        var result = await _repository.GetByDateRangeAsync(
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        Assert.Empty(result);
    }
}
