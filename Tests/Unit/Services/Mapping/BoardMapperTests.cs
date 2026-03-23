using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per BoardMapper.
/// </summary>
public class BoardMapperTests
{
    private readonly BoardTypeEntity _boardTypeEntity = new()
    {
        Id = 1,
        Name = "Madre Optimus",
        FirmwareType = 17
    };

    [Fact]
    public void ToDomain_WithBoardType_MapsAllProperties()
    {
        var entity = CreateEntity(isPrimary: false);
        var boardType = new BoardType("Madre Optimus", 17);

        var result = BoardMapper.ToDomain(entity, boardType);

        Assert.Equal(10, result.Id);
        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
        Assert.Equal("Madre Optimus", result.BoardType.Name);
        Assert.Equal("Test Board", result.Name);
        Assert.Equal(1, result.BoardNumber);
        Assert.Equal("DIS001", result.PartNumber);
        Assert.False(result.IsPrimary);
    }

    [Fact]
    public void ToDomain_IsPrimaryTrue_MapsCorrectly()
    {
        var entity = CreateEntity(isPrimary: true);
        var boardType = new BoardType("Madre Optimus", 17);

        var result = BoardMapper.ToDomain(entity, boardType);

        Assert.True(result.IsPrimary);
    }

    [Fact]
    public void ToDomain_WithNavigation_MapsAllProperties()
    {
        var entity = CreateEntity(isPrimary: true);
        entity.BoardType = _boardTypeEntity;

        var result = BoardMapper.ToDomain(entity);

        Assert.True(result.IsPrimary);
        Assert.Equal("Madre Optimus", result.BoardType.Name);
    }

    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        var boardType = BoardType.Restore(1, "Madre Optimus", 17);
        var board = new Board(DeviceType.OptimusXp, boardType, "Madre", 1,
            "DIS001", isPrimary: true);

        var result = BoardMapper.ToEntity(board);

        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
        Assert.Equal(1, result.BoardTypeId);
        Assert.Equal("Madre", result.Name);
        Assert.Equal(1, result.BoardNumber);
        Assert.Equal("DIS001", result.PartNumber);
        Assert.True(result.IsPrimary);
    }

    [Fact]
    public void ToEntity_IsPrimaryFalse_MapsCorrectly()
    {
        var boardType = BoardType.Restore(1, "Madre Optimus", 17);
        var board = new Board(DeviceType.OptimusXp, boardType, "Periferica", 2);

        var result = BoardMapper.ToEntity(board);

        Assert.False(result.IsPrimary);
    }

    [Fact]
    public void UpdateEntity_UpdatesAllFields()
    {
        var entity = CreateEntity(isPrimary: false);
        var boardType = BoardType.Restore(1, "Madre Optimus", 17);
        var updated = Board.Restore(10, DeviceType.EdenXp, boardType, "Renamed", 2, "NEW", true);

        BoardMapper.UpdateEntity(entity, updated);

        Assert.Equal(DeviceType.EdenXp, entity.DeviceType);
        Assert.Equal("Renamed", entity.Name);
        Assert.Equal(2, entity.BoardNumber);
        Assert.Equal("NEW", entity.PartNumber);
        Assert.True(entity.IsPrimary);
    }

    private BoardEntity CreateEntity(bool isPrimary) => new()
    {
        Id = 10,
        DeviceType = DeviceType.OptimusXp,
        BoardTypeId = 1,
        Name = "Test Board",
        BoardNumber = 1,
        PartNumber = "DIS001",
        ProtocolAddress = 0,
        IsPrimary = isPrimary
    };
}
