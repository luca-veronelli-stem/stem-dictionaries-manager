using Core.Enums;

namespace Tests.Unit.Enums;

/// <summary>
/// Test per AccessMode enum.
/// </summary>
public class AccessModeTests
{
    [Fact]
    public void AccessMode_HasExpectedCount()
    {
        var values = Enum.GetValues<AccessMode>();
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void AccessMode_ContainsAllExpectedValues()
    {
        Assert.True(Enum.IsDefined(AccessMode.ReadOnly));
        Assert.True(Enum.IsDefined(AccessMode.ReadWrite));
        Assert.True(Enum.IsDefined(AccessMode.WriteOnly));
    }
}
