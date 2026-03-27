using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per DeviceMapper.
/// </summary>
public class DeviceMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsDevice()
    {
        var entity = new DeviceEntity
        {
            Id = 3,
            Name = "Eden-XP",
            MachineCode = 3,
            Description = "Supporto barella"
        };

        var result = DeviceMapper.ToDomain(entity);

        Assert.Equal(3, result.Id);
        Assert.Equal("Eden-XP", result.Name);
        Assert.Equal(3, result.MachineCode);
        Assert.Equal("Supporto barella", result.Description);
    }

    [Fact]
    public void ToDomain_NullDescription_MapsNull()
    {
        var entity = new DeviceEntity
        {
            Id = 1,
            Name = "Spark",
            MachineCode = 7,
            Description = null
        };

        var result = DeviceMapper.ToDomain(entity);

        Assert.Null(result.Description);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => DeviceMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        var device = Device.Restore(5, "Spyke", 5, "Barella con caricamento");

        var result = DeviceMapper.ToEntity(device);

        Assert.Equal(5, result.Id);
        Assert.Equal("Spyke", result.Name);
        Assert.Equal(5, result.MachineCode);
        Assert.Equal("Barella con caricamento", result.Description);
    }

    [Fact]
    public void ToEntity_NullDescription_MapsNull()
    {
        var device = Device.Restore(1, "Spark", 7, null);

        var result = DeviceMapper.ToEntity(device);

        Assert.Null(result.Description);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => DeviceMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_UpdatesAllProperties()
    {
        var entity = new DeviceEntity
        {
            Id = 1,
            Name = "Old",
            MachineCode = 1,
            Description = "Old desc"
        };
        var domain = Device.Restore(1, "New Name", 99, "New desc");

        DeviceMapper.UpdateEntity(entity, domain);

        Assert.Equal("New Name", entity.Name);
        Assert.Equal(99, entity.MachineCode);
        Assert.Equal("New desc", entity.Description);
    }

    [Fact]
    public void UpdateEntity_NullEntity_ThrowsArgumentNullException()
    {
        var domain = Device.Restore(1, "Test", 1, null);

        Assert.Throws<ArgumentNullException>(
            () => DeviceMapper.UpdateEntity(null!, domain));
    }

    [Fact]
    public void UpdateEntity_NullDomain_ThrowsArgumentNullException()
    {
        var entity = new DeviceEntity { Id = 1, Name = "Test", MachineCode = 1 };

        Assert.Throws<ArgumentNullException>(
            () => DeviceMapper.UpdateEntity(entity, null!));
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllDevices()
    {
        var entities = new[]
        {
            new DeviceEntity { Id = 1, Name = "A", MachineCode = 1 },
            new DeviceEntity { Id = 2, Name = "B", MachineCode = 2 },
            new DeviceEntity { Id = 3, Name = "C", MachineCode = 3 }
        };

        var result = DeviceMapper.ToDomainList(entities);

        Assert.Equal(3, result.Count);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("C", result[2].Name);
    }

    [Fact]
    public void ToDomainList_EmptyList_ReturnsEmpty()
    {
        var result = DeviceMapper.ToDomainList([]);

        Assert.Empty(result);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        var original = new DeviceEntity
        {
            Id = 10,
            Name = "R3L-XP",
            MachineCode = 11,
            Description = "Supporto barella elettromeccanico"
        };

        var domain = DeviceMapper.ToDomain(original);
        var roundTripped = DeviceMapper.ToEntity(domain);

        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.Name, roundTripped.Name);
        Assert.Equal(original.MachineCode, roundTripped.MachineCode);
        Assert.Equal(original.Description, roundTripped.Description);
    }
}
