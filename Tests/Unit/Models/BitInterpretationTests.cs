using Core.Enums;
using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per BitInterpretation model.
/// </summary>
public class BitInterpretationTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesBitInterpretation()
    {
        var interpretation = new BitInterpretation(
            variableId: 6,
            deviceType: DeviceType.EdenBs8,
            wordIndex: 0,
            bitIndex: 0,
            meaning: "sovracorrente pompa");

        Assert.Equal(6, interpretation.VariableId);
        Assert.Equal(DeviceType.EdenBs8, interpretation.DeviceType);
        Assert.Equal(0, interpretation.WordIndex);
        Assert.Equal(0, interpretation.BitIndex);
        Assert.Equal("sovracorrente pompa", interpretation.Meaning);
        Assert.Equal(0, interpretation.Id);
    }

    [Fact]
    public void Constructor_NegativeWordIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new BitInterpretation(6, DeviceType.EdenBs8, -1, 0, "test"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    [InlineData(100)]
    public void Constructor_InvalidBitIndex_ThrowsArgumentOutOfRangeException(int bitIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new BitInterpretation(6, DeviceType.EdenBs8, 0, bitIndex, "test"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void Constructor_ValidBitIndex_IsAccepted(int bitIndex)
    {
        var interpretation = new BitInterpretation(6, DeviceType.EdenBs8, 0, bitIndex, "test");

        Assert.Equal(bitIndex, interpretation.BitIndex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidMeaning_ThrowsArgumentException(string meaning)
    {
        Assert.Throws<ArgumentException>(() => 
            new BitInterpretation(6, DeviceType.EdenBs8, 0, 0, meaning));
    }

    [Fact]
    public void Constructor_NullMeaning_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new BitInterpretation(6, DeviceType.EdenBs8, 0, 0, null!));
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var interpretation = BitInterpretation.Restore(
            id: 99,
            variableId: 6,
            deviceType: DeviceType.SherpaSlim,
            wordIndex: 1,
            bitIndex: 5,
            meaning: "fusibile aperto");

        Assert.Equal(99, interpretation.Id);
        Assert.Equal(6, interpretation.VariableId);
        Assert.Equal(DeviceType.SherpaSlim, interpretation.DeviceType);
        Assert.Equal(1, interpretation.WordIndex);
        Assert.Equal(5, interpretation.BitIndex);
    }
}
