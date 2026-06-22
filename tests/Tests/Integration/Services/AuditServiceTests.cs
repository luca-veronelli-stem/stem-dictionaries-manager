using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Services;
using Tests.Shared;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per AuditService.
/// </summary>
public class AuditServiceTests : IntegrationTestBase
{
    private readonly AuditService _service;
    private UserEntity _testUser = null!;

    public AuditServiceTests()
    {
        var repository = new AuditEntryRepository(Context, NullLogger<RepositoryBase<AuditEntryEntity>>.Instance);
        _service = new AuditService(repository, NullLogger<AuditService>.Instance);
    }

    public override async Task InitializeAsync()
    {
        _testUser = TestData.CreateAdmin();
        Context.Users.Add(_testUser);
        await Context.SaveChangesAsync();
    }

    // === LogCreateAsync ===

    [Fact]
    public async Task LogCreateAsync_ValidInput_CreatesEntry()
    {
        // Act
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 1, _testUser.Id,
            "{\"name\":\"TestVar\"}");

        // Assert
        IReadOnlyList<AuditEntry> entries = await _service.GetByEntityAsync(
            AuditEntityType.Variable, 1);
        Assert.Single(entries);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Equal("{\"name\":\"TestVar\"}", entries[0].NewValue);
        Assert.Null(entries[0].PreviousValue);
    }

    [Fact]
    public async Task LogCreateAsync_WithNotes_PersistsNotes()
    {
        await _service.LogCreateAsync(
            AuditEntityType.Dictionary, 5, _testUser.Id,
            "{}", "Creato dizionario Standard");

        IReadOnlyList<AuditEntry> entries = await _service.GetByEntityAsync(
            AuditEntityType.Dictionary, 5);
        Assert.Equal("Creato dizionario Standard", entries[0].Notes);
    }

    [Fact]
    public async Task LogCreateAsync_NullJson_ThrowsArgumentException()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.LogCreateAsync(
                AuditEntityType.Variable, 1, _testUser.Id, null!));
    }

    [Fact]
    public async Task LogCreateAsync_EmptyJson_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.LogCreateAsync(
                AuditEntityType.Variable, 1, _testUser.Id, "  "));
    }

    // === LogUpdateAsync ===

    [Fact]
    public async Task LogUpdateAsync_ValidInput_CreatesEntry()
    {
        await _service.LogUpdateAsync(
            AuditEntityType.Command, 3, _testUser.Id,
            "{\"name\":\"old\"}", "{\"name\":\"new\"}");

        IReadOnlyList<AuditEntry> entries = await _service.GetByEntityAsync(
            AuditEntityType.Command, 3);
        Assert.Single(entries);
        Assert.Equal(AuditOperation.Update, entries[0].Operation);
        Assert.Equal("{\"name\":\"old\"}", entries[0].PreviousValue);
        Assert.Equal("{\"name\":\"new\"}", entries[0].NewValue);
    }

    [Fact]
    public async Task LogUpdateAsync_NullPreviousJson_ThrowsArgumentException()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.LogUpdateAsync(
                AuditEntityType.Variable, 1, _testUser.Id,
                null!, "{\"name\":\"new\"}"));
    }

    [Fact]
    public async Task LogUpdateAsync_NullNewJson_ThrowsArgumentException()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.LogUpdateAsync(
                AuditEntityType.Variable, 1, _testUser.Id,
                "{\"name\":\"old\"}", null!));
    }

    // === LogDeleteAsync ===

    [Fact]
    public async Task LogDeleteAsync_ValidInput_CreatesEntry()
    {
        await _service.LogDeleteAsync(
            AuditEntityType.Board, 7, _testUser.Id,
            "{\"name\":\"Madre\"}");

        IReadOnlyList<AuditEntry> entries = await _service.GetByEntityAsync(
            AuditEntityType.Board, 7);
        Assert.Single(entries);
        Assert.Equal(AuditOperation.Delete, entries[0].Operation);
        Assert.Equal("{\"name\":\"Madre\"}", entries[0].PreviousValue);
        Assert.Null(entries[0].NewValue);
    }

    [Fact]
    public async Task LogDeleteAsync_NullPreviousJson_ThrowsArgumentException()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.LogDeleteAsync(
                AuditEntityType.Variable, 1, _testUser.Id, null!));
    }

    // === GetByIdAsync ===

    [Fact]
    public async Task GetByIdAsync_ExistingEntry_ReturnsEntry()
    {
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 10, _testUser.Id, "{}");

        IReadOnlyList<AuditEntry> all = await _service.GetRecentAsync(1);
        AuditEntry? result = await _service.GetByIdAsync(all[0].Id);

        Assert.NotNull(result);
        Assert.Equal(AuditEntityType.Variable, result.EntityType);
        Assert.Equal(10, result.EntityId);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        AuditEntry? result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    // === GetByEntityAsync ===

    [Fact]
    public async Task GetByEntityAsync_FiltersCorrectly()
    {
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 42, _testUser.Id, "{}");
        await _service.LogUpdateAsync(
            AuditEntityType.Variable, 42, _testUser.Id, "{}", "{}");
        await _service.LogCreateAsync(
            AuditEntityType.Command, 42, _testUser.Id, "{}");

        IReadOnlyList<AuditEntry> result = await _service.GetByEntityAsync(
            AuditEntityType.Variable, 42);

        Assert.Equal(2, result.Count);
        Assert.All(result, e =>
            Assert.Equal(AuditEntityType.Variable, e.EntityType));
    }

    [Fact]
    public async Task GetByEntityAsync_OrderedByDateDescending()
    {
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 1, _testUser.Id, "{\"v\":1}");
        await Task.Delay(50);
        await _service.LogUpdateAsync(
            AuditEntityType.Variable, 1, _testUser.Id, "{}", "{\"v\":2}");

        IReadOnlyList<AuditEntry> result = await _service.GetByEntityAsync(
            AuditEntityType.Variable, 1);

        Assert.Equal(AuditOperation.Update, result[0].Operation);
        Assert.Equal(AuditOperation.Create, result[1].Operation);
    }

    // === GetByUserAsync ===

    [Fact]
    public async Task GetByUserAsync_FiltersCorrectly()
    {
        UserEntity otherUser = TestData.CreateUser("other", "Other User");
        Context.Users.Add(otherUser);
        await Context.SaveChangesAsync();

        await _service.LogCreateAsync(
            AuditEntityType.Variable, 1, _testUser.Id, "{}");
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 2, otherUser.Id, "{}");
        await _service.LogCreateAsync(
            AuditEntityType.Command, 1, _testUser.Id, "{}");

        IReadOnlyList<AuditEntry> result = await _service.GetByUserAsync(_testUser.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(_testUser.Id, e.ChangedById));
    }

    // === GetRecentAsync ===

    [Fact]
    public async Task GetRecentAsync_ReturnsLimitedCount()
    {
        for (int i = 0; i < 5; i++)
        {
            await _service.LogCreateAsync(
                AuditEntityType.Variable, i, _testUser.Id, "{}");
        }

        IReadOnlyList<AuditEntry> result = await _service.GetRecentAsync(3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetRecentAsync_OrderedByDateDescending()
    {
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 1, _testUser.Id, "{\"first\":true}");
        await Task.Delay(50);
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 2, _testUser.Id, "{\"second\":true}");

        IReadOnlyList<AuditEntry> result = await _service.GetRecentAsync(10);

        Assert.Equal(2, result[0].EntityId);
        Assert.Equal(1, result[1].EntityId);
    }

    // === GetByDateRangeAsync ===

    [Fact]
    public async Task GetByDateRangeAsync_FiltersCorrectly()
    {
        // Arrange — seed direttamente per controllare le date
        var repo = new AuditEntryRepository(Context, NullLogger<RepositoryBase<AuditEntryEntity>>.Instance);
        await repo.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 1,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            NewValue = "{}"
        });
        await repo.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 2,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            NewValue = "{}"
        });
        await repo.AddAsync(new AuditEntryEntity
        {
            EntityType = AuditEntityType.Variable,
            EntityId = 3,
            Operation = AuditOperation.Create,
            ChangedById = _testUser.Id,
            ChangedAt = new DateTime(2026, 12, 31, 23, 0, 0, DateTimeKind.Utc),
            NewValue = "{}"
        });

        // Act
        IReadOnlyList<AuditEntry> result = await _service.GetByDateRangeAsync(
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].EntityId);
    }

    [Fact]
    public async Task GetByDateRangeAsync_FromAfterTo_ThrowsArgumentException()
    {
        var to = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var from = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetByDateRangeAsync(from, to));
    }

    [Fact]
    public async Task GetByDateRangeAsync_NoMatches_ReturnsEmpty()
    {
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 1, _testUser.Id, "{}");

        IReadOnlyList<AuditEntry> result = await _service.GetByDateRangeAsync(
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.Empty(result);
    }

    // === Full Audit Trail Scenario ===

    [Fact]
    public async Task FullTrail_CreateUpdateDelete_TracksAllOperations()
    {
        // Create
        await _service.LogCreateAsync(
            AuditEntityType.Variable, 50, _testUser.Id,
            "{\"name\":\"TestVar\"}", "Creata variabile");

        // Update
        await _service.LogUpdateAsync(
            AuditEntityType.Variable, 50, _testUser.Id,
            "{\"name\":\"TestVar\"}", "{\"name\":\"TestVar_v2\"}",
            "Rinominata");

        // Delete
        await _service.LogDeleteAsync(
            AuditEntityType.Variable, 50, _testUser.Id,
            "{\"name\":\"TestVar_v2\"}", "Rimossa variabile");

        // Assert
        IReadOnlyList<AuditEntry> trail = await _service.GetByEntityAsync(
            AuditEntityType.Variable, 50);

        Assert.Equal(3, trail.Count);
        // Ordine discendente (più recente prima)
        Assert.Equal(AuditOperation.Delete, trail[0].Operation);
        Assert.Equal(AuditOperation.Update, trail[1].Operation);
        Assert.Equal(AuditOperation.Create, trail[2].Operation);
        // Notes preservate
        Assert.Equal("Rimossa variabile", trail[0].Notes);
        Assert.Equal("Rinominata", trail[1].Notes);
        Assert.Equal("Creata variabile", trail[2].Notes);
    }
}
