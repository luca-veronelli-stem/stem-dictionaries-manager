using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per BitInterpretationMapper.
/// </summary>
public class BitInterpretationMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsInterpretation()
    {
        var entity = new BitInterpretationEntity
        {
            Id = 1,
            VariableId = 10,
            DeviceType = DeviceType.OptimusXp,
            WordIndex = 0,
            BitIndex = 5,
            Meaning = "Motor Running"
        };

        var result = BitInterpretationMapper.ToDomain(entity);

        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.VariableId);
        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
        Assert.Equal(0, result.WordIndex);
        Assert.Equal(5, result.BitIndex);
        Assert.Equal("Motor Running", result.Meaning);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            BitInterpretationMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        var domain = BitInterpretation.Restore(1, 10, DeviceType.Eden, 1, 3, "Error Flag");

        var result = BitInterpretationMapper.ToEntity(domain);

        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.VariableId);
        Assert.Equal(DeviceType.Eden, result.DeviceType);
        Assert.Equal(1, result.WordIndex);
        Assert.Equal(3, result.BitIndex);
        Assert.Equal("Error Flag", result.Meaning);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            BitInterpretationMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        var entity = new BitInterpretationEntity
        {
            Id = 1,
            VariableId = 10,
            DeviceType = DeviceType.Spark,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Original"
        };
        var domain = BitInterpretation.Restore(1, 20, DeviceType.Gradino, 1, 7, "Updated");

        BitInterpretationMapper.UpdateEntity(entity, domain);

        Assert.Equal(20, entity.VariableId);
        Assert.Equal(DeviceType.Gradino, entity.DeviceType);
        Assert.Equal(1, entity.WordIndex);
        Assert.Equal(7, entity.BitIndex);
        Assert.Equal("Updated", entity.Meaning);
    }

    [Fact]
    public void UpdateEntity_NullEntity_ThrowsArgumentNullException()
    {
        var domain = BitInterpretation.Restore(1, 10, DeviceType.Eden, 0, 0, "Test");

        Assert.Throws<ArgumentNullException>(() => 
            BitInterpretationMapper.UpdateEntity(null!, domain));
    }

    [Fact]
    public void UpdateEntity_NullDomain_ThrowsArgumentNullException()
    {
        var entity = new BitInterpretationEntity();

        Assert.Throws<ArgumentNullException>(() => 
            BitInterpretationMapper.UpdateEntity(entity, null!));
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        var entities = new List<BitInterpretationEntity>
        {
            new() { Id = 1, VariableId = 10, DeviceType = DeviceType.OptimusXp, 
                    WordIndex = 0, BitIndex = 0, Meaning = "Bit0" },
            new() { Id = 2, VariableId = 10, DeviceType = DeviceType.OptimusXp, 
                    WordIndex = 0, BitIndex = 1, Meaning = "Bit1" },
            new() { Id = 3, VariableId = 10, DeviceType = DeviceType.OptimusXp, 
                    WordIndex = 0, BitIndex = 2, Meaning = "Bit2" }
        };

        var result = BitInterpretationMapper.ToDomainList(entities);

        Assert.Equal(3, result.Count);
        Assert.Equal("Bit0", result[0].Meaning);
        Assert.Equal("Bit1", result[1].Meaning);
        Assert.Equal("Bit2", result[2].Meaning);
    }

    [Fact]
    public void ToDomainList_EmptyList_ReturnsEmptyList()
    {
        var result = BitInterpretationMapper.ToDomainList([]);

        Assert.Empty(result);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        var original = new BitInterpretationEntity
        {
            Id = 42,
            VariableId = 100,
            DeviceType = DeviceType.EdenBs8,
            WordIndex = 2,
            BitIndex = 15,
            Meaning = "Maximum Bit"
        };

        var domain = BitInterpretationMapper.ToDomain(original);
        var roundTrip = BitInterpretationMapper.ToEntity(domain);

        Assert.Equal(original.Id, roundTrip.Id);
        Assert.Equal(original.VariableId, roundTrip.VariableId);
        Assert.Equal(original.DeviceType, roundTrip.DeviceType);
        Assert.Equal(original.WordIndex, roundTrip.WordIndex);
        Assert.Equal(original.BitIndex, roundTrip.BitIndex);
        Assert.Equal(original.Meaning, roundTrip.Meaning);
    }
}
