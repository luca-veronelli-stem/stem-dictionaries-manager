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
            dataType: "uint16_t",
            accessMode: AccessMode.ReadOnly);

        Assert.Equal("Firmware macchina", variable.Name);
        Assert.Equal(0x00, variable.AddressHigh);
        Assert.Equal(0x00, variable.AddressLow);
        Assert.Equal("uint16_t", variable.DataType);
        Assert.Equal(AccessMode.ReadOnly, variable.AccessMode);
        Assert.True(variable.IsEnabled);
        Assert.Equal(0, variable.Id);
    }

    [Fact]
    public void Constructor_WithAllOptionalParameters()
    {
        var variable = new Variable(
            name: "Livello batteria",
            addressHigh: 0x00,
            addressLow: 0x0F,
            dataType: "uint16_t",
            accessMode: AccessMode.ReadOnly,
            isEnabled: true,
            format: null,
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
        var variable = new Variable("Test", 0x80, 0x15, "uint8_t", AccessMode.ReadOnly);

        Assert.Equal(0x8015, variable.FullAddress);
    }

    [Theory]
    [InlineData(0x00, VariableCategory.Standard)]
    [InlineData(0x80, VariableCategory.DeviceSpecific)]
    public void Category_IsDerivedFromAddressHigh(byte addressHigh, VariableCategory expected)
    {
        var variable = new Variable("Test", addressHigh, 0x00, "uint8_t", AccessMode.ReadOnly);

        Assert.Equal(expected, variable.Category);
    }

    [Fact]
    public void Enable_SetsIsEnabledTrue()
    {
        var variable = new Variable("Test", 0x00, 0x00, "uint8_t", AccessMode.ReadOnly, isEnabled: false);
        
        variable.Enable();

        Assert.True(variable.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledFalse()
    {
        var variable = new Variable("Test", 0x00, 0x00, "uint8_t", AccessMode.ReadOnly, isEnabled: true);
        
        variable.Disable();

        Assert.False(variable.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => 
            new Variable(name, 0x00, 0x00, "uint8_t", AccessMode.ReadOnly));
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new Variable(null!, 0x00, 0x00, "uint8_t", AccessMode.ReadOnly));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidDataType_ThrowsArgumentException(string dataType)
    {
        Assert.Throws<ArgumentException>(() => 
            new Variable("Test", 0x00, 0x00, dataType, AccessMode.ReadOnly));
    }

    [Fact]
    public void Restore_SetsIdAndAllProperties()
    {
        var variable = Variable.Restore(
            id: 42,
            name: "Test",
            addressHigh: 0x80,
            addressLow: 0x10,
            dataType: "float",
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
        Assert.False(variable.IsEnabled);
        Assert.Equal("255.255", variable.Format);
    }
}
