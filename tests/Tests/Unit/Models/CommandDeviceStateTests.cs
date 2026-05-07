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
        var state = new CommandDeviceState(5, 10);

        Assert.Equal(5, state.CommandId);
        Assert.Equal(10, state.DeviceId);
        Assert.True(state.IsEnabled);
        Assert.Equal(0, state.Id);
    }

    [Fact]
    public void Constructor_DisabledState()
    {
        var state = new CommandDeviceState(5, 10, isEnabled: false);

        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Enable_SetsIsEnabledTrue()
    {
        var state = new CommandDeviceState(5, 10, isEnabled: false);

        state.Enable();

        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledFalse()
    {
        var state = new CommandDeviceState(5, 10, isEnabled: true);

        state.Disable();

        Assert.False(state.IsEnabled);
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var state = CommandDeviceState.Restore(77, 5, 12, false);

        Assert.Equal(77, state.Id);
        Assert.Equal(5, state.CommandId);
        Assert.Equal(12, state.DeviceId);
        Assert.False(state.IsEnabled);
    }
}
