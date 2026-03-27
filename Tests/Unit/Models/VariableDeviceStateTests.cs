using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Unit test per VariableDeviceState domain model.
/// SESSION_035: deviceId enum → int DeviceId.
/// </summary>
public class VariableDeviceStateTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var state = new VariableDeviceState(10, 5, true);

        Assert.Equal(0, state.Id);
        Assert.Equal(10, state.VariableId);
        Assert.Equal(5, state.DeviceId);
        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Constructor_DefaultIsEnabled_True()
    {
        var state = new VariableDeviceState(5, 7);

        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Constructor_IsEnabled_False()
    {
        var state = new VariableDeviceState(5, 1, false);

        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Restore_SetsAllProperties()
    {
        var state = VariableDeviceState.Restore(42, 10, 12, false);

        Assert.Equal(42, state.Id);
        Assert.Equal(10, state.VariableId);
        Assert.Equal(12, state.DeviceId);
        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Enable_SetsIsEnabledTrue()
    {
        var state = new VariableDeviceState(1, 4, false);

        state.Enable();

        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledFalse()
    {
        var state = new VariableDeviceState(1, 4, true);

        state.Disable();

        Assert.False(state.IsEnabled);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(99)]
    public void Constructor_VariousDeviceIds_Work(int deviceId)
    {
        var state = new VariableDeviceState(1, deviceId);

        Assert.Equal(deviceId, state.DeviceId);
    }
}
