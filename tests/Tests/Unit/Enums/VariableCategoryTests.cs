using Core.Enums;

namespace Tests.Unit.Enums;

/// <summary>
/// Test per VariableCategory enum.
/// </summary>
public class VariableCategoryTests
{
    [Fact]
    public void VariableCategory_HasExpectedCount()
    {
        VariableCategory[] values = Enum.GetValues<VariableCategory>();
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void VariableCategory_ContainsStandardAndDeviceSpecific()
    {
        Assert.True(Enum.IsDefined(VariableCategory.Standard));
        Assert.True(Enum.IsDefined(VariableCategory.DeviceSpecific));
    }
}
