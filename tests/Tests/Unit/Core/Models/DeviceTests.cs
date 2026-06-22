using Core.Models;

namespace Tests.Unit.Core.Models;

/// <summary>
/// Test per Device model.
/// SESSION_035: Device entity sostituisce DeviceType enum.
/// </summary>
public class DeviceTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesDevice()
    {
        var device = new Device("Sherpa Slim", 1);

        Assert.Equal("Sherpa Slim", device.Name);
        Assert.Equal(1, device.MachineCode);
        Assert.Null(device.Description);
        Assert.Equal(0, device.Id);
    }

    [Fact]
    public void Constructor_WithDescription_SetsDescription()
    {
        var device = new Device("Eden-XP", 3, "Supporto barella");

        Assert.Equal("Eden-XP", device.Name);
        Assert.Equal(3, device.MachineCode);
        Assert.Equal("Supporto barella", device.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_InvalidName_ThrowsArgumentException(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Device(name!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_InvalidMachineCode_ThrowsArgumentOutOfRangeException(
        int machineCode)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Device("Test", machineCode));
    }

    [Fact]
    public void Constructor_ReservedBleModuleMachineCode_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => new Device("BLE Module", Device.ReservedBleModuleMachineCode));
    }

    [Fact]
    public void ReservedBleModuleMachineCode_IsSix()
    {
        Assert.Equal(6, Device.ReservedBleModuleMachineCode);
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var device = Device.Restore(42, "Optimus-XP", 10, "Supporto");

        Assert.Equal(42, device.Id);
        Assert.Equal("Optimus-XP", device.Name);
        Assert.Equal(10, device.MachineCode);
        Assert.Equal("Supporto", device.Description);
    }

    [Fact]
    public void Restore_NullDescription_SetsNull()
    {
        var device = Device.Restore(1, "Spark", 7, null);

        Assert.Null(device.Description);
    }

    [Fact]
    public void Restore_InvalidMachineCode_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Device.Restore(1, "Bad", 0, null));
    }

    [Fact]
    public void Restore_ReservedMachineCode_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => Device.Restore(1, "BLE", 6, null));
    }
}
