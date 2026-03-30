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
            wordIndex: 0,
            bitIndex: 0,
            meaning: "sovracorrente pompa",
            deviceId: null);

        Assert.Equal(6, interpretation.VariableId);
        Assert.Equal(0, interpretation.WordIndex);
        Assert.Equal(0, interpretation.BitIndex);
        Assert.Equal("sovracorrente pompa", interpretation.Meaning);
        Assert.Equal(0, interpretation.Id);
    }

    [Fact]
    public void Constructor_NegativeWordIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(6, -1, 0, "test", null));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    [InlineData(100)]
    public void Constructor_InvalidBitIndex_ThrowsArgumentOutOfRangeException(int bitIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(6, 0, bitIndex, "test", null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void Constructor_ValidBitIndex_IsAccepted(int bitIndex)
    {
        var interpretation = new BitInterpretation(6, 0, bitIndex, "test", null);

        Assert.Equal(bitIndex, interpretation.BitIndex);
    }

    [Fact]
    public void Constructor_NullMeaning_IsAccepted()
    {
        var interpretation = new BitInterpretation(6, 0, 0, null, null);

        Assert.Null(interpretation.Meaning);
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var interpretation = BitInterpretation.Restore(
            id: 99,
            variableId: 6,
            wordIndex: 1,
            bitIndex: 5,
            meaning: "fusibile aperto",
            deviceId: null);

        Assert.Equal(99, interpretation.Id);
        Assert.Equal(6, interpretation.VariableId);
        Assert.Equal(1, interpretation.WordIndex);
        Assert.Equal(5, interpretation.BitIndex);
    }

    // === DeviceId Tests (SESSION_037) ===

    [Fact]
    public void Constructor_WithNullDeviceId_HasNullDeviceId()
    {
        var interpretation = new BitInterpretation(6, 0, 0, "test", deviceId: null);

        Assert.Null(interpretation.DeviceId);
    }

    [Fact]
    public void Constructor_WithDeviceId_SetsDeviceId()
    {
        var interpretation = new BitInterpretation(6, 0, 0, "test", deviceId: 42);

        Assert.Equal(42, interpretation.DeviceId);
    }

    [Fact]
    public void Constructor_WithNullDeviceId_SetsNull()
    {
        var interpretation = new BitInterpretation(6, 0, 0, "test", deviceId: null);

        Assert.Null(interpretation.DeviceId);
    }

    [Fact]
    public void Restore_WithDeviceId_SetsDeviceId()
    {
        var interpretation = BitInterpretation.Restore(
            id: 1, variableId: 6, wordIndex: 0, bitIndex: 0,
            meaning: "test", deviceId: 99);

        Assert.Equal(99, interpretation.DeviceId);
    }

    [Fact]
    public void Restore_WithNullDeviceId_HasNullDeviceId()
    {
        var interpretation = BitInterpretation.Restore(
            id: 1, variableId: 6, wordIndex: 0, bitIndex: 0,
            meaning: "test", deviceId: null);

        Assert.Null(interpretation.DeviceId);
    }

    [Fact]
    public void Constructor_WithDeviceId_StillValidatesBitIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(6, 0, 16, "test", deviceId: 1));
    }

    [Fact]
    public void Constructor_WithDeviceId_StillValidatesWordIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(6, -1, 0, "test", deviceId: 1));
    }
}
