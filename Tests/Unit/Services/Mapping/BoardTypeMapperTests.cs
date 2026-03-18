using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per BoardTypeMapper.
/// </summary>
public class BoardTypeMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsBoardType()
    {
        // Arrange
        var entity = new BoardTypeEntity
        {
            Id = 1,
            Name = "Madre",
            FirmwareType = 17
        };

        // Act
        var result = BoardTypeMapper.ToDomain(entity);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Madre", result.Name);
        Assert.Equal(17, result.FirmwareType);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BoardTypeMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        // Arrange
        var boardType = BoardType.Restore(5, "Pulsantiera", 4);

        // Act
        var result = BoardTypeMapper.ToEntity(boardType);

        // Assert
        Assert.Equal(5, result.Id);
        Assert.Equal("Pulsantiera", result.Name);
        Assert.Equal(4, result.FirmwareType);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BoardTypeMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        // Arrange
        var entity = new BoardTypeEntity
        {
            Id = 1,
            Name = "OldName",
            FirmwareType = 10
        };
        var domain = BoardType.Restore(1, "NewName", 20);

        // Act
        BoardTypeMapper.UpdateEntity(entity, domain);

        // Assert
        Assert.Equal("NewName", entity.Name);
        Assert.Equal(20, entity.FirmwareType);
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void UpdateEntity_NullEntity_ThrowsArgumentNullException()
    {
        var domain = new BoardType("Test", 1);
        Assert.Throws<ArgumentNullException>(() => BoardTypeMapper.UpdateEntity(null!, domain));
    }

    [Fact]
    public void UpdateEntity_NullDomain_ThrowsArgumentNullException()
    {
        var entity = new BoardTypeEntity { Id = 1, Name = "Test", FirmwareType = 1 };
        Assert.Throws<ArgumentNullException>(() => BoardTypeMapper.UpdateEntity(entity, null!));
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        // Arrange
        var entities = new List<BoardTypeEntity>
        {
            new() { Id = 1, Name = "Madre", FirmwareType = 17 },
            new() { Id = 2, Name = "Pulsantiera", FirmwareType = 4 },
            new() { Id = 3, Name = "R3lXpMaster", FirmwareType = 25 }
        };

        // Act
        var result = BoardTypeMapper.ToDomainList(entities);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Madre", result[0].Name);
        Assert.Equal("Pulsantiera", result[1].Name);
        Assert.Equal("R3lXpMaster", result[2].Name);
    }

    [Fact]
    public void ToDomainList_EmptyList_ReturnsEmptyList()
    {
        var result = BoardTypeMapper.ToDomainList([]);
        Assert.Empty(result);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        // Arrange
        var originalEntity = new BoardTypeEntity
        {
            Id = 42,
            Name = "TestBoard",
            FirmwareType = 99
        };

        // Act
        var domain = BoardTypeMapper.ToDomain(originalEntity);
        var resultEntity = BoardTypeMapper.ToEntity(domain);

        // Assert
        Assert.Equal(originalEntity.Id, resultEntity.Id);
        Assert.Equal(originalEntity.Name, resultEntity.Name);
        Assert.Equal(originalEntity.FirmwareType, resultEntity.FirmwareType);
    }
}
