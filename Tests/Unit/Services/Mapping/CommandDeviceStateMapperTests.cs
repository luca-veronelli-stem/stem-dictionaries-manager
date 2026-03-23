using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per CommandDeviceStateMapper.
/// </summary>
public class CommandDeviceStateMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsState()
    {
        var entity = new CommandDeviceStateEntity
        {
            Id = 1,
            CommandId = 10,
            DeviceType = DeviceType.OptimusXp,
            IsEnabled = true
        };

        var result = CommandDeviceStateMapper.ToDomain(entity);

        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.CommandId);
        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public void ToDomain_DisabledState_ReturnsCorrectly()
    {
        var entity = new CommandDeviceStateEntity
        {
            Id = 2,
            CommandId = 20,
            DeviceType = DeviceType.EdenXp,
            IsEnabled = false
        };

        var result = CommandDeviceStateMapper.ToDomain(entity);

        Assert.False(result.IsEnabled);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CommandDeviceStateMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        var domain = CommandDeviceState.Restore(1, 10, DeviceType.Spark, true);

        var result = CommandDeviceStateMapper.ToEntity(domain);

        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.CommandId);
        Assert.Equal(DeviceType.Spark, result.DeviceType);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CommandDeviceStateMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        var entity = new CommandDeviceStateEntity
        {
            Id = 1,
            CommandId = 10,
            DeviceType = DeviceType.Gradino,
            IsEnabled = false
        };
        var domain = CommandDeviceState.Restore(1, 20, DeviceType.Spyke, true);

        CommandDeviceStateMapper.UpdateEntity(entity, domain);

        Assert.Equal(20, entity.CommandId);
        Assert.Equal(DeviceType.Spyke, entity.DeviceType);
        Assert.True(entity.IsEnabled);
    }

    [Fact]
    public void UpdateEntity_NullEntity_ThrowsArgumentNullException()
    {
        var domain = CommandDeviceState.Restore(1, 10, DeviceType.EdenXp, true);

        Assert.Throws<ArgumentNullException>(() =>
            CommandDeviceStateMapper.UpdateEntity(null!, domain));
    }

    [Fact]
    public void UpdateEntity_NullDomain_ThrowsArgumentNullException()
    {
        var entity = new CommandDeviceStateEntity();

        Assert.Throws<ArgumentNullException>(() =>
            CommandDeviceStateMapper.UpdateEntity(entity, null!));
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        var entities = new List<CommandDeviceStateEntity>
        {
            new() { Id = 1, CommandId = 10, DeviceType = DeviceType.OptimusXp, IsEnabled = true },
            new() { Id = 2, CommandId = 10, DeviceType = DeviceType.EdenXp, IsEnabled = false },
            new() { Id = 3, CommandId = 10, DeviceType = DeviceType.Spark, IsEnabled = true }
        };

        var result = CommandDeviceStateMapper.ToDomainList(entities);

        Assert.Equal(3, result.Count);
        Assert.True(result[0].IsEnabled);
        Assert.False(result[1].IsEnabled);
        Assert.True(result[2].IsEnabled);
    }

    [Fact]
    public void ToDomainList_EmptyList_ReturnsEmptyList()
    {
        var result = CommandDeviceStateMapper.ToDomainList([]);

        Assert.Empty(result);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        var original = new CommandDeviceStateEntity
        {
            Id = 42,
            CommandId = 100,
            DeviceType = DeviceType.R3lXp,
            IsEnabled = true
        };

        var domain = CommandDeviceStateMapper.ToDomain(original);
        var roundTrip = CommandDeviceStateMapper.ToEntity(domain);

        Assert.Equal(original.Id, roundTrip.Id);
        Assert.Equal(original.CommandId, roundTrip.CommandId);
        Assert.Equal(original.DeviceType, roundTrip.DeviceType);
        Assert.Equal(original.IsEnabled, roundTrip.IsEnabled);
    }
}
