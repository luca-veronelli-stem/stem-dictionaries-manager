using Core.Enums;
using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Unit test per VariableDeviceState domain model.
/// Speculare a CommandDeviceStateTests.
/// </summary>
public class VariableDeviceStateTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var state = new VariableDeviceState(10, DeviceType.OptimusXp, true);

        Assert.Equal(0, state.Id);
        Assert.Equal(10, state.VariableId);
        Assert.Equal(DeviceType.OptimusXp, state.DeviceType);
        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Constructor_DefaultIsEnabled_True()
    {
        var state = new VariableDeviceState(5, DeviceType.Spark);

        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Constructor_IsEnabled_False()
    {
        var state = new VariableDeviceState(5, DeviceType.SherpaSlim, false);

        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Restore_SetsAllProperties()
    {
        var state = VariableDeviceState.Restore(42, 10, DeviceType.EdenBs8, false);

        Assert.Equal(42, state.Id);
        Assert.Equal(10, state.VariableId);
        Assert.Equal(DeviceType.EdenBs8, state.DeviceType);
        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Enable_SetsIsEnabledTrue()
    {
        var state = new VariableDeviceState(1, DeviceType.Gradino, false);

        state.Enable();

        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledFalse()
    {
        var state = new VariableDeviceState(1, DeviceType.Gradino, true);

        state.Disable();

        Assert.False(state.IsEnabled);
    }

    [Theory]
    [InlineData(DeviceType.SherpaSlim)]
    [InlineData(DeviceType.TopLiftM)]
    [InlineData(DeviceType.EdenXp)]
    [InlineData(DeviceType.Gradino)]
    [InlineData(DeviceType.Spyke)]
    [InlineData(DeviceType.Spark)]
    [InlineData(DeviceType.TopLiftA2)]
    [InlineData(DeviceType.O3zTech)]
    [InlineData(DeviceType.OptimusXp)]
    [InlineData(DeviceType.R3lXp)]
    [InlineData(DeviceType.EdenBs8)]
    public void Constructor_AllDeviceTypes_Work(DeviceType deviceType)
    {
        var state = new VariableDeviceState(1, deviceType);

        Assert.Equal(deviceType, state.DeviceType);
    }
}
