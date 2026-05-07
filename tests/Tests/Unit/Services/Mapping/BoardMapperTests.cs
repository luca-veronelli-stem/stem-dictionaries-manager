using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per BoardMapper (Domain v2).
/// </summary>
public class BoardMapperTests
{
    [Fact]
    public void ToDomain_MapsAllProperties()
    {
        BoardEntity entity = CreateEntity(isPrimary: false, dictionaryId: 5);

        Board result = BoardMapper.ToDomain(entity);

        Assert.Equal(10, result.Id);
        Assert.Equal(10, result.DeviceId);
        Assert.Equal("Test Board", result.Name);
        Assert.Equal(17, result.FirmwareType);
        Assert.Equal(1, result.BoardNumber);
        Assert.Equal("DIS001", result.PartNumber);
        Assert.False(result.IsPrimary);
        Assert.Equal(5, result.DictionaryId);
    }

    [Fact]
    public void ToDomain_IsPrimaryTrue_MapsCorrectly()
    {
        BoardEntity entity = CreateEntity(isPrimary: true);

        Board result = BoardMapper.ToDomain(entity);

        Assert.True(result.IsPrimary);
    }

    [Fact]
    public void ToDomain_NullDictionaryId_MapsNull()
    {
        BoardEntity entity = CreateEntity(isPrimary: false, dictionaryId: null);

        Board result = BoardMapper.ToDomain(entity);

        Assert.Null(result.DictionaryId);
    }

    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        var board = new Board(10, "Madre", 17, 1,
            10, "DIS001", isPrimary: true, dictionaryId: 3);

        BoardEntity result = BoardMapper.ToEntity(board);

        Assert.Equal(10, result.DeviceId);
        Assert.Equal("Madre", result.Name);
        Assert.Equal(17, result.FirmwareType);
        Assert.Equal(1, result.BoardNumber);
        Assert.Equal("DIS001", result.PartNumber);
        Assert.True(result.IsPrimary);
        Assert.Equal(3, result.DictionaryId);
    }

    [Fact]
    public void ToEntity_IsPrimaryFalse_MapsCorrectly()
    {
        var board = new Board(10, "Periferica", 4, 2, 10);

        BoardEntity result = BoardMapper.ToEntity(board);

        Assert.False(result.IsPrimary);
        Assert.Null(result.DictionaryId);
    }

    [Fact]
    public void UpdateEntity_UpdatesAllFields()
    {
        BoardEntity entity = CreateEntity(isPrimary: false);
        var updated = Board.Restore(10, 3, "Renamed", 18, 2, "NEW", true, 7, machineCode: 3);

        BoardMapper.UpdateEntity(entity, updated);

        Assert.Equal(3, entity.DeviceId);
        Assert.Equal("Renamed", entity.Name);
        Assert.Equal(18, entity.FirmwareType);
        Assert.Equal(2, entity.BoardNumber);
        Assert.Equal("NEW", entity.PartNumber);
        Assert.True(entity.IsPrimary);
        Assert.Equal(7, entity.DictionaryId);
    }

    [Fact]
    public void ToDomain_WithDictionaryNavigation_MapsDictionaryName()
    {
        BoardEntity entity = CreateEntity(isPrimary: false, dictionaryId: 5);
        entity.Dictionary = new DictionaryEntity { Id = 5, Name = "Standard" };

        Board result = BoardMapper.ToDomain(entity);

        Assert.Equal("Standard", result.DictionaryName);
    }

    [Fact]
    public void ToDomain_WithoutDictionaryNavigation_DictionaryNameIsNull()
    {
        BoardEntity entity = CreateEntity(isPrimary: false, dictionaryId: 5);
        // Dictionary navigation is null (not loaded)

        Board result = BoardMapper.ToDomain(entity);

        Assert.Null(result.DictionaryName);
    }

    private static BoardEntity CreateEntity(bool isPrimary, int? dictionaryId = null) => new()
    {
        Id = 10,
        DeviceId = 10,
        Name = "Test Board",
        FirmwareType = 17,
        BoardNumber = 1,
        PartNumber = "DIS001",
        ProtocolAddress = 0,
        IsPrimary = isPrimary,
        DictionaryId = dictionaryId,
        Device = new DeviceEntity { Id = 10, Name = "Optimus-XP", MachineCode = 10 }
    };
}
