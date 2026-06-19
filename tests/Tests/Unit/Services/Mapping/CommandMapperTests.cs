using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per CommandMapper. La (de)serializzazione JSON dei parametri e'
/// ora gestita dalla value conversion EF Core: il mapper copia solo la lista.
/// </summary>
public class CommandMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsCommand()
    {
        // Arrange
        var entity = new CommandEntity
        {
            Id = 1,
            Name = "READ_VARIABLE",
            CodeHigh = 0x01,
            CodeLow = 0x00,
            IsResponse = false,
            Parameters = ["address", "length"]
        };

        // Act
        Command result = CommandMapper.ToDomain(entity);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("READ_VARIABLE", result.Name);
        Assert.Equal(0x01, result.CodeHigh);
        Assert.Equal(0x00, result.CodeLow);
        Assert.False(result.IsResponse);
        Assert.Equal(2, result.Parameters.Count);
        Assert.Contains("address", result.Parameters);
        Assert.Contains("length", result.Parameters);
    }

    [Fact]
    public void ToDomain_ResponseCommand_HasIsResponseTrue()
    {
        // Arrange
        var entity = new CommandEntity
        {
            Id = 2,
            Name = "READ_VARIABLE_RESPONSE",
            CodeHigh = 0x01,
            CodeLow = 0x00,
            IsResponse = true,
            Parameters = ["data"]
        };

        // Act
        Command result = CommandMapper.ToDomain(entity);

        // Assert
        Assert.True(result.IsResponse);
        Assert.Single(result.Parameters);
    }

    [Fact]
    public void ToDomain_EmptyParameters_ReturnsEmptyList()
    {
        // Arrange
        var entity = new CommandEntity
        {
            Id = 3,
            Name = "PING",
            CodeHigh = 0x00,
            CodeLow = 0x00,
            IsResponse = false,
            Parameters = []
        };

        // Act
        Command result = CommandMapper.ToDomain(entity);

        // Assert
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void ToDomain_NullParameters_ReturnsEmptyList()
    {
        // Arrange — defensive: a null list maps to an empty parameter set.
        var entity = new CommandEntity
        {
            Id = 4,
            Name = "NOOP",
            CodeHigh = 0xFF,
            CodeLow = 0xFF,
            IsResponse = false,
            Parameters = null!
        };

        // Act
        Command result = CommandMapper.ToDomain(entity);

        // Assert
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CommandMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        // Arrange
        var command = Command.Restore(
            id: 10,
            name: "WRITE_VARIABLE",
            codeHigh: 0x01,
            codeLow: 0x01,
            isResponse: false,
            parameters: ["address", "value"]);

        // Act
        CommandEntity result = CommandMapper.ToEntity(command);

        // Assert
        Assert.Equal(10, result.Id);
        Assert.Equal("WRITE_VARIABLE", result.Name);
        Assert.Equal(0x01, result.CodeHigh);
        Assert.Equal(0x01, result.CodeLow);
        Assert.False(result.IsResponse);
        Assert.Equal(["address", "value"], result.Parameters);
    }

    [Fact]
    public void ToEntity_NoParameters_ReturnsEmptyList()
    {
        // Arrange
        var command = Command.Restore(
            id: 11,
            name: "HEARTBEAT",
            codeHigh: 0x00,
            codeLow: 0x00,
            isResponse: false,
            parameters: []);

        // Act
        CommandEntity result = CommandMapper.ToEntity(command);

        // Assert
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CommandMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        // Arrange
        var entity = new CommandEntity
        {
            Id = 1,
            Name = "OldName",
            CodeHigh = 0x00,
            CodeLow = 0x00,
            IsResponse = false,
            Parameters = []
        };

        var domain = Command.Restore(
            id: 1,
            name: "NewName",
            codeHigh: 0xFF,
            codeLow: 0xFE,
            isResponse: true,
            parameters: ["param1", "param2", "param3"]);

        // Act
        CommandMapper.UpdateEntity(entity, domain);

        // Assert
        Assert.Equal("NewName", entity.Name);
        Assert.Equal(0xFF, entity.CodeHigh);
        Assert.Equal(0xFE, entity.CodeLow);
        Assert.True(entity.IsResponse);
        Assert.Equal(["param1", "param2", "param3"], entity.Parameters);
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        // Arrange
        var entities = new List<CommandEntity>
        {
            new() { Id = 1, Name = "CMD1", CodeHigh = 0x01, CodeLow = 0x00,
                    IsResponse = false, Parameters = ["a"] },
            new() { Id = 2, Name = "CMD2", CodeHigh = 0x02, CodeLow = 0x00,
                    IsResponse = false, Parameters = ["b", "c"] },
            new() { Id = 3, Name = "CMD3", CodeHigh = 0x01, CodeLow = 0x00,
                    IsResponse = true, Parameters = [] }
        };

        // Act
        IReadOnlyList<Command> result = CommandMapper.ToDomainList(entities);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("CMD1", result[0].Name);
        Assert.Single(result[0].Parameters);
        Assert.Equal("CMD2", result[1].Name);
        Assert.Equal(2, result[1].Parameters.Count);
        Assert.Equal("CMD3", result[2].Name);
        Assert.True(result[2].IsResponse);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        // Arrange
        var originalEntity = new CommandEntity
        {
            Id = 42,
            Name = "ROUNDTRIP_CMD",
            CodeHigh = 0xAB,
            CodeLow = 0xCD,
            IsResponse = true,
            Parameters = ["x", "y", "z"]
        };

        // Act
        Command domain = CommandMapper.ToDomain(originalEntity);
        CommandEntity resultEntity = CommandMapper.ToEntity(domain);

        // Assert
        Assert.Equal(originalEntity.Id, resultEntity.Id);
        Assert.Equal(originalEntity.Name, resultEntity.Name);
        Assert.Equal(originalEntity.CodeHigh, resultEntity.CodeHigh);
        Assert.Equal(originalEntity.CodeLow, resultEntity.CodeLow);
        Assert.Equal(originalEntity.IsResponse, resultEntity.IsResponse);
        Assert.Equal(["x", "y", "z"], resultEntity.Parameters);
    }

    [Fact]
    public void FullCode_AfterMapping_IsCorrect()
    {
        // Arrange
        var entity = new CommandEntity
        {
            Id = 1,
            Name = "TEST",
            CodeHigh = 0x12,
            CodeLow = 0x34,
            IsResponse = false,
            Parameters = []
        };

        // Act
        Command result = CommandMapper.ToDomain(entity);

        // Assert
        Assert.Equal(0x1234, result.FullCode);
    }
}
