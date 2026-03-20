using Core.Enums;
using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per AuditEntry model.
/// </summary>
public class AuditEntryTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesAuditEntry()
    {
        var entry = new AuditEntry(
            AuditEntityType.Variable,
            42,
            AuditOperation.Update,
            1);

        Assert.Equal(AuditEntityType.Variable, entry.EntityType);
        Assert.Equal(42, entry.EntityId);
        Assert.Equal(AuditOperation.Update, entry.Operation);
        Assert.Equal(1, entry.ChangedById);
        Assert.True(entry.ChangedAt <= DateTime.UtcNow);
        Assert.Equal(0, entry.Id);
    }

    [Fact]
    public void Constructor_WithAllParameters()
    {
        var timestamp = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var entry = new AuditEntry(
            AuditEntityType.Variable,
            42,
            AuditOperation.Update,
            1,
            timestamp,
            "{\"name\":\"old\"}",
            "{\"name\":\"new\"}",
            "Test notes");

        Assert.Equal(timestamp, entry.ChangedAt);
        Assert.Equal("{\"name\":\"old\"}", entry.PreviousValue);
        Assert.Equal("{\"name\":\"new\"}", entry.NewValue);
        Assert.Equal("Test notes", entry.Notes);
    }

    [Fact]
    public void ForCreate_CreatesCorrectEntry()
    {
        var entry = AuditEntry.ForCreate(AuditEntityType.Variable, 10, 1, "{\"name\":\"Test\"}");

        Assert.Equal(AuditOperation.Create, entry.Operation);
        Assert.Null(entry.PreviousValue);
        Assert.Equal("{\"name\":\"Test\"}", entry.NewValue);
    }

    [Fact]
    public void ForUpdate_CreatesCorrectEntry()
    {
        var entry = AuditEntry.ForUpdate(
            AuditEntityType.Variable, 10, 1,
            "{\"name\":\"old\"}", "{\"name\":\"new\"}", "Updated name");

        Assert.Equal(AuditOperation.Update, entry.Operation);
        Assert.Equal("{\"name\":\"old\"}", entry.PreviousValue);
        Assert.Equal("{\"name\":\"new\"}", entry.NewValue);
        Assert.Equal("Updated name", entry.Notes);
    }

    [Fact]
    public void ForDelete_CreatesCorrectEntry()
    {
        var entry = AuditEntry.ForDelete(AuditEntityType.Variable, 10, 1, "{\"name\":\"deleted\"}");

        Assert.Equal(AuditOperation.Delete, entry.Operation);
        Assert.Equal("{\"name\":\"deleted\"}", entry.PreviousValue);
        Assert.Null(entry.NewValue);
    }

    [Fact]
    public void Restore_SetsIdAndAllProperties()
    {
        var timestamp = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var entry = AuditEntry.Restore(
            99,
            AuditEntityType.Command,
            50,
            AuditOperation.Delete,
            2,
            timestamp,
            "{\"old\":true}",
            null,
            "Deleted by admin");

        Assert.Equal(99, entry.Id);
        Assert.Equal(AuditEntityType.Command, entry.EntityType);
        Assert.Equal(50, entry.EntityId);
        Assert.Equal(AuditOperation.Delete, entry.Operation);
        Assert.Equal(2, entry.ChangedById);
        Assert.Equal(timestamp, entry.ChangedAt);
    }
}
