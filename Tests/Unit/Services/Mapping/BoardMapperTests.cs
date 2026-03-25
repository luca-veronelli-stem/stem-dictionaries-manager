using Core.Enums;
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
        var entity = CreateEntity(isPrimary: false, dictionaryId: 5);

        var result = BoardMapper.ToDomain(entity);

        Assert.Equal(10, result.Id);
        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
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
        var entity = CreateEntity(isPrimary: true);

        var result = BoardMapper.ToDomain(entity);

        Assert.True(result.IsPrimary);
    }

    [Fact]
    public void ToDomain_NullDictionaryId_MapsNull()
    {
        var entity = CreateEntity(isPrimary: false, dictionaryId: null);

        var result = BoardMapper.ToDomain(entity);

        Assert.Null(result.DictionaryId);
    }

    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        var board = new Board(DeviceType.OptimusXp, "Madre", 17, 1,
            "DIS001", isPrimary: true, dictionaryId: 3);

        var result = BoardMapper.ToEntity(board);

        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
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
        var board = new Board(DeviceType.OptimusXp, "Periferica", 4, 2);

        var result = BoardMapper.ToEntity(board);

        Assert.False(result.IsPrimary);
        Assert.Null(result.DictionaryId);
    }

    [Fact]
    public void UpdateEntity_UpdatesAllFields()
    {
        var entity = CreateEntity(isPrimary: false);
        var updated = Board.Restore(10, DeviceType.EdenXp, "Renamed", 18, 2, "NEW", true, 7);

        BoardMapper.UpdateEntity(entity, updated);

        Assert.Equal(DeviceType.EdenXp, entity.DeviceType);
        Assert.Equal("Renamed", entity.Name);
        Assert.Equal(18, entity.FirmwareType);
        Assert.Equal(2, entity.BoardNumber);
        Assert.Equal("NEW", entity.PartNumber);
        Assert.True(entity.IsPrimary);
        Assert.Equal(7, entity.DictionaryId);
    }

    private static BoardEntity CreateEntity(bool isPrimary, int? dictionaryId = null) => new()
    {
        Id = 10,
        DeviceType = DeviceType.OptimusXp,
        Name = "Test Board",
        FirmwareType = 17,
        BoardNumber = 1,
        PartNumber = "DIS001",
        ProtocolAddress = 0,
        IsPrimary = isPrimary,
        DictionaryId = dictionaryId
    };
}
