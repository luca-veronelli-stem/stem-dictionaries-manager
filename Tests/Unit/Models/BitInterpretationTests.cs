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
            dictionaryId: null);

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
            new BitInterpretation(6, -1, 0, "test", dictionaryId: null));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    [InlineData(100)]
    public void Constructor_InvalidBitIndex_ThrowsArgumentOutOfRangeException(int bitIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(6, 0, bitIndex, "test", dictionaryId: null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void Constructor_ValidBitIndex_IsAccepted(int bitIndex)
    {
        var interpretation = new BitInterpretation(6, 0, bitIndex, "test", dictionaryId: null);

        Assert.Equal(bitIndex, interpretation.BitIndex);
    }

    [Fact]
    public void Constructor_NullMeaning_IsAccepted()
    {
        var interpretation = new BitInterpretation(6, 0, 0, null, dictionaryId: null);

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
            dictionaryId: null);

        Assert.Equal(99, interpretation.Id);
        Assert.Equal(6, interpretation.VariableId);
        Assert.Equal(1, interpretation.WordIndex);
        Assert.Equal(5, interpretation.BitIndex);
    }

    // === DictionaryId Tests (v7) ===

    [Fact]
    public void Constructor_WithNullDictionaryId_HasNullDictionaryId()
    {
        var interpretation = new BitInterpretation(6, 0, 0, "test", dictionaryId: null);

        Assert.Null(interpretation.DictionaryId);
    }

    [Fact]
    public void Constructor_WithDictionaryId_SetsDictionaryId()
    {
        var interpretation = new BitInterpretation(6, 0, 0, "test", dictionaryId: 42);

        Assert.Equal(42, interpretation.DictionaryId);
    }

    [Fact]
    public void Restore_WithDictionaryId_SetsDictionaryId()
    {
        var interpretation = BitInterpretation.Restore(
            id: 1, variableId: 6, wordIndex: 0, bitIndex: 0,
            meaning: "test", dictionaryId: 99);

        Assert.Equal(99, interpretation.DictionaryId);
    }

    [Fact]
    public void Restore_WithNullDictionaryId_HasNullDictionaryId()
    {
        var interpretation = BitInterpretation.Restore(
            id: 1, variableId: 6, wordIndex: 0, bitIndex: 0,
            meaning: "test", dictionaryId: null);

        Assert.Null(interpretation.DictionaryId);
    }

    [Fact]
    public void Constructor_WithDictionaryId_StillValidatesBitIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(6, 0, 16, "test", dictionaryId: 1));
    }

    [Fact]
    public void Constructor_WithDictionaryId_StillValidatesWordIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(6, -1, 0, "test", dictionaryId: 1));
    }

    // === maxBitIndex parametrico (BR-007 v6) ===

    [Theory]
    [InlineData(7, 7)]
    [InlineData(31, 31)]
    public void Constructor_CustomMaxBitIndex_AcceptsUpToMax(int maxBitIndex, int bitIndex)
    {
        var bi = new BitInterpretation(1, 0, bitIndex, "test", dictionaryId: null, maxBitIndex);

        Assert.Equal(bitIndex, bi.BitIndex);
    }

    [Fact]
    public void Constructor_MaxBitIndex7_RejectsBitIndex8()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(1, 0, 8, "test", dictionaryId: null, maxBitIndex: 7));
    }

    [Fact]
    public void Constructor_MaxBitIndex31_AcceptsBitIndex31()
    {
        var bi = new BitInterpretation(1, 0, 31, "test", dictionaryId: null, maxBitIndex: 31);

        Assert.Equal(31, bi.BitIndex);
    }

    [Fact]
    public void Constructor_MaxBitIndex31_RejectsBitIndex32()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(1, 0, 32, "test", dictionaryId: null, maxBitIndex: 31));
    }

    [Fact]
    public void Constructor_DefaultMaxBitIndex_Is15()
    {
        // BitIndex 15 accettato (default)
        var bi = new BitInterpretation(1, 0, 15, "test", dictionaryId: null);
        Assert.Equal(15, bi.BitIndex);

        // BitIndex 16 rifiutato (default)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BitInterpretation(1, 0, 16, "test", dictionaryId: null));
    }
}
