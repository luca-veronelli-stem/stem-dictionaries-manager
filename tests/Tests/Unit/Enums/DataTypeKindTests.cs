using Core.Enums;

namespace Tests.Unit.Enums;

/// <summary>
/// Test per DataTypeKind enum.
/// </summary>
public class DataTypeKindTests
{
    [Fact]
    public void DataTypeKind_HasExpectedCount()
    {
        DataTypeKind[] values = Enum.GetValues<DataTypeKind>();
        Assert.Equal(12, values.Length);
    }

    [Fact]
    public void DataTypeKind_ContainsAllPrimitiveTypes()
    {
        Assert.True(Enum.IsDefined(DataTypeKind.UInt8));
        Assert.True(Enum.IsDefined(DataTypeKind.UInt16));
        Assert.True(Enum.IsDefined(DataTypeKind.UInt32));
        Assert.True(Enum.IsDefined(DataTypeKind.Int8));
        Assert.True(Enum.IsDefined(DataTypeKind.Int16));
        Assert.True(Enum.IsDefined(DataTypeKind.Int32));
        Assert.True(Enum.IsDefined(DataTypeKind.Float));
        Assert.True(Enum.IsDefined(DataTypeKind.Bool));
    }

    [Fact]
    public void DataTypeKind_ContainsComplexTypes()
    {
        Assert.True(Enum.IsDefined(DataTypeKind.String));
        Assert.True(Enum.IsDefined(DataTypeKind.Bitmapped));
        Assert.True(Enum.IsDefined(DataTypeKind.Array));
        Assert.True(Enum.IsDefined(DataTypeKind.Other));
    }
}
