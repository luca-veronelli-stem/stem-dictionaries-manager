using Core.Enums;
using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per CommandDeviceState model.
/// </summary>
public class CommandDeviceStateTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesState()
    {
        var state = new CommandDeviceState(5, DeviceType.OptimusXp);

        Assert.Equal(5, state.CommandId);
        Assert.Equal(DeviceType.OptimusXp, state.DeviceType);
        Assert.True(state.IsEnabled);
        Assert.Equal(0, state.Id);
    }

    [Fact]
    public void Constructor_DisabledState()
    {
        var state = new CommandDeviceState(5, DeviceType.OptimusXp, isEnabled: false);

        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Enable_SetsIsEnabledTrue()
    {
        var state = new CommandDeviceState(5, DeviceType.OptimusXp, isEnabled: false);

        state.Enable();

        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledFalse()
    {
        var state = new CommandDeviceState(5, DeviceType.OptimusXp, isEnabled: true);

        state.Disable();

        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var state = CommandDeviceState.Restore(77, 5, DeviceType.EdenBs8, false);

        Assert.Equal(77, state.Id);
        Assert.Equal(5, state.CommandId);
        Assert.Equal(DeviceType.EdenBs8, state.DeviceType);
        Assert.False(state.IsEnabled);
    }
}
