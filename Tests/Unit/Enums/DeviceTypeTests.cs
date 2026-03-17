using Core.Enums;

namespace Tests.Unit.Enums;

/// <summary>
/// Test per DeviceType enum.
/// Verifica che i valori corrispondano ai codici MACHINE del protocollo.
/// </summary>
public class DeviceTypeTests
{
    [Fact]
    public void DeviceType_HasExpectedCount()
    {
        var values = Enum.GetValues<DeviceType>();
        Assert.Equal(12, values.Length);
    }

    [Theory]
    [InlineData(DeviceType.SherpaSlim, 1)]
    [InlineData(DeviceType.Optimus, 2)]
    [InlineData(DeviceType.Eden, 3)]
    [InlineData(DeviceType.Gradino, 4)]
    [InlineData(DeviceType.Spyke, 5)]
    [InlineData(DeviceType.BleModule, 6)]
    [InlineData(DeviceType.Spark, 7)]
    [InlineData(DeviceType.TopLiftA2, 8)]
    [InlineData(DeviceType.O3zTech, 9)]
    [InlineData(DeviceType.OptimusXp, 10)]
    [InlineData(DeviceType.R3lXp, 11)]
    [InlineData(DeviceType.EdenBs8, 12)]
    public void DeviceType_HasCorrectMachineCode(DeviceType deviceType, int expectedCode)
    {
        Assert.Equal(expectedCode, (int)deviceType);
    }

    [Fact]
    public void DeviceType_AllValuesAreUnique()
    {
        var values = Enum.GetValues<DeviceType>().Cast<int>().ToList();
        Assert.Equal(values.Count, values.Distinct().Count());
    }
}
