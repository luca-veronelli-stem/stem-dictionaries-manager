using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit test per VariableDeviceStateMapper.
/// Speculare a CommandDeviceStateMapperTests.
/// </summary>
public class VariableDeviceStateMapperTests
{
    [Fact]
    public void ToDomain_MapsAllProperties()
    {
        var entity = new VariableDeviceStateEntity
        {
            Id = 1, VariableId = 10, DeviceType = DeviceType.OptimusXp, IsEnabled = false
        };

        var domain = VariableDeviceStateMapper.ToDomain(entity);

        Assert.Equal(1, domain.Id);
        Assert.Equal(10, domain.VariableId);
        Assert.Equal(DeviceType.OptimusXp, domain.DeviceType);
        Assert.False(domain.IsEnabled);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VariableDeviceStateMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        var domain = VariableDeviceState.Restore(5, 20, DeviceType.Spark, true);

        var entity = VariableDeviceStateMapper.ToEntity(domain);

        Assert.Equal(5, entity.Id);
        Assert.Equal(20, entity.VariableId);
        Assert.Equal(DeviceType.Spark, entity.DeviceType);
        Assert.True(entity.IsEnabled);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VariableDeviceStateMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_UpdatesAllProperties()
    {
        var entity = new VariableDeviceStateEntity
        {
            Id = 1, VariableId = 10, DeviceType = DeviceType.OptimusXp, IsEnabled = true
        };
        var domain = VariableDeviceState.Restore(1, 20, DeviceType.Spark, false);

        VariableDeviceStateMapper.UpdateEntity(entity, domain);

        Assert.Equal(20, entity.VariableId);
        Assert.Equal(DeviceType.Spark, entity.DeviceType);
        Assert.False(entity.IsEnabled);
    }

    [Fact]
    public void UpdateEntity_NullEntity_ThrowsArgumentNullException()
    {
        var domain = VariableDeviceState.Restore(1, 10, DeviceType.Spark, true);
        Assert.Throws<ArgumentNullException>(() => VariableDeviceStateMapper.UpdateEntity(null!, domain));
    }

    [Fact]
    public void UpdateEntity_NullDomain_ThrowsArgumentNullException()
    {
        var entity = new VariableDeviceStateEntity();
        Assert.Throws<ArgumentNullException>(() => VariableDeviceStateMapper.UpdateEntity(entity, null!));
    }

    [Fact]
    public void ToDomainList_ConvertsAll()
    {
        var entities = new[]
        {
            new VariableDeviceStateEntity { Id = 1, VariableId = 10, DeviceType = DeviceType.SherpaSlim, IsEnabled = false },
            new VariableDeviceStateEntity { Id = 2, VariableId = 10, DeviceType = DeviceType.EdenXp, IsEnabled = true }
        };

        var result = VariableDeviceStateMapper.ToDomainList(entities);

        Assert.Equal(2, result.Count);
        Assert.Equal(DeviceType.SherpaSlim, result[0].DeviceType);
        Assert.Equal(DeviceType.EdenXp, result[1].DeviceType);
    }

    [Fact]
    public void ToDomainList_EmptyList_ReturnsEmpty()
    {
        var result = VariableDeviceStateMapper.ToDomainList([]);

        Assert.Empty(result);
    }
}
