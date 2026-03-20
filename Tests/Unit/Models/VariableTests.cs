using Core.Enums;
using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per Variable model.
/// </summary>
public class VariableTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesVariable()
    {
        var variable = new Variable(
            name: "Firmware macchina",
            addressHigh: 0x00,
            addressLow: 0x00,
            dataTypeKind: DataTypeKind.UInt16,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "uint16_t");

        Assert.Equal("Firmware macchina", variable.Name);
        Assert.Equal(0x00, variable.AddressHigh);
        Assert.Equal(0x00, variable.AddressLow);
        Assert.Equal(DataTypeKind.UInt16, variable.DataTypeKind);
        Assert.Equal("uint16_t", variable.DataTypeRaw);
        Assert.Null(variable.DataTypeParam);
        Assert.Equal(AccessMode.ReadOnly, variable.AccessMode);
        Assert.True(variable.IsEnabled);
        Assert.Equal(0, variable.Id);
    }

    [Fact]
    public void Constructor_WithDataTypeParam_ForString()
    {
        var variable = new Variable(
            name: "Modello",
            addressHigh: 0x00,
            addressLow: 0x02,
            dataTypeKind: DataTypeKind.String,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "String[20]",
            dataTypeParam: 20);

        Assert.Equal(DataTypeKind.String, variable.DataTypeKind);
        Assert.Equal(20, variable.DataTypeParam);
        Assert.Equal("String[20]", variable.DataTypeRaw);
    }

    [Fact]
    public void Constructor_WithDataTypeParam_ForBitmapped()
    {
        var variable = new Variable(
            name: "Allarmi",
            addressHigh: 0x00,
            addressLow: 0x06,
            dataTypeKind: DataTypeKind.Bitmapped,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "due word uint16_t bitmapped",
            dataTypeParam: 2);

        Assert.Equal(DataTypeKind.Bitmapped, variable.DataTypeKind);
        Assert.Equal(2, variable.DataTypeParam);
    }

    [Fact]
    public void Constructor_WithDataTypeParam_ForArray()
    {
        var variable = new Variable(
            name: "Stato",
            addressHigh: 0x00,
            addressLow: 0x05,
            dataTypeKind: DataTypeKind.Array,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "3*uint32_t",
            dataTypeParam: 3);

        Assert.Equal(DataTypeKind.Array, variable.DataTypeKind);
        Assert.Equal(3, variable.DataTypeParam);
    }

    [Fact]
    public void Constructor_WithAllOptionalParameters()
    {
        var variable = new Variable(
            name: "Livello batteria",
            addressHigh: 0x00,
            addressLow: 0x0F,
            dataTypeKind: DataTypeKind.UInt16,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "uint16_t",
            isEnabled: true,
            minValue: 0,
            maxValue: 100,
            unit: "%",
            usage: "Diagnostica",
            description: "Percentuale carica batteria");

        Assert.Equal(0, variable.MinValue);
        Assert.Equal(100, variable.MaxValue);
        Assert.Equal("%", variable.Unit);
        Assert.Equal("Diagnostica", variable.Usage);
        Assert.Equal("Percentuale carica batteria", variable.Description);
    }

    [Fact]
    public void FullAddress_ReturnsCorrectValue()
    {
        var variable = new Variable("Test", 0x80, 0x15, DataTypeKind.UInt8,
            AccessMode.ReadOnly, "uint8_t");

        Assert.Equal(0x8015, variable.FullAddress);
    }

    [Theory]
    [InlineData(0x00, VariableCategory.Standard)]
    [InlineData(0x80, VariableCategory.DeviceSpecific)]
    public void Category_IsDerivedFromAddressHigh(byte addressHigh, VariableCategory expected)
    {
        var variable = new Variable("Test", addressHigh, 0x00, DataTypeKind.UInt8,
            AccessMode.ReadOnly, "uint8_t");

        Assert.Equal(expected, variable.Category);
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x40)]
    [InlineData(0xFF)]
    public void Constructor_InvalidAddressHigh_ThrowsArgumentOutOfRangeException(byte addressHigh)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Variable("Test", addressHigh, 0x00, DataTypeKind.UInt8,
                AccessMode.ReadOnly, "uint8_t"));
    }

    [Fact]
    public void Enable_SetsIsEnabledTrue()
    {
        var variable = new Variable("Test", 0x00, 0x00, DataTypeKind.UInt8,
            AccessMode.ReadOnly, "uint8_t", isEnabled: false);

        variable.Enable();

        Assert.True(variable.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledFalse()
    {
        var variable = new Variable("Test", 0x00, 0x00, DataTypeKind.UInt8,
            AccessMode.ReadOnly, "uint8_t", isEnabled: true);

        variable.Disable();

        Assert.False(variable.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Variable(name, 0x00, 0x00, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Variable(null!, 0x00, 0x00, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidDataTypeRaw_ThrowsArgumentException(string dataTypeRaw)
    {
        Assert.Throws<ArgumentException>(() =>
            new Variable("Test", 0x00, 0x00, DataTypeKind.UInt8, AccessMode.ReadOnly, dataTypeRaw));
    }

    [Fact]
    public void Constructor_NullDataTypeRaw_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Variable("Test", 0x00, 0x00, DataTypeKind.UInt8, AccessMode.ReadOnly, null!));
    }

    [Fact]
    public void Restore_SetsIdAndAllProperties()
    {
        var variable = Variable.Restore(
            id: 42,
            name: "Test",
            addressHigh: 0x80,
            addressLow: 0x10,
            dataTypeKind: DataTypeKind.Float,
            dataTypeRaw: "float",
            dataTypeParam: null,
            accessMode: AccessMode.ReadWrite,
            isEnabled: false,
            format: "255.255",
            minValue: -100,
            maxValue: 100,
            unit: "mm",
            usage: "Test usage",
            description: "Test description");

        Assert.Equal(42, variable.Id);
        Assert.Equal("Test", variable.Name);
        Assert.Equal(0x80, variable.AddressHigh);
        Assert.Equal(DataTypeKind.Float, variable.DataTypeKind);
        Assert.Equal("float", variable.DataTypeRaw);
        Assert.False(variable.IsEnabled);
        Assert.Equal("255.255", variable.Format);
    }
}
