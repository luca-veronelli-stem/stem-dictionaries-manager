#if WINDOWS
using GUI.Windows.ViewModels;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per WordBitGroup.
/// </summary>
public class WordBitGroupTests
{
    [Fact]
    public void Constructor_SetsWordIndexAndLabel()
    {
        var group = new WordBitGroup(2);

        Assert.Equal(2, group.WordIndex);
        Assert.Equal("Word 2", group.Label);
    }

    [Fact]
    public void TryAddBit_FirstBit_HasBitIndexZero()
    {
        var group = new WordBitGroup(0);

        var result = group.TryAddBit();

        Assert.True(result);
        Assert.Single(group.Items);
        Assert.Equal(0, group.Items[0].BitIndex);
        Assert.Equal(0, group.Items[0].WordIndex);
        Assert.Equal(string.Empty, group.Items[0].Meaning);
    }

    [Fact]
    public void TryAddBit_IncrementsBitIndex()
    {
        var group = new WordBitGroup(1);
        group.TryAddBit(); // BitIndex = 0
        group.TryAddBit(); // BitIndex = 1
        group.TryAddBit(); // BitIndex = 2

        Assert.Equal(3, group.Items.Count);
        Assert.Equal(0, group.Items[0].BitIndex);
        Assert.Equal(1, group.Items[1].BitIndex);
        Assert.Equal(2, group.Items[2].BitIndex);
    }

    [Fact]
    public void TryAddBit_ReturnsFalse_When16BitsReached()
    {
        var group = new WordBitGroup(0);
        for (var i = 0; i < 16; i++)
            group.TryAddBit();

        Assert.Equal(16, group.Items.Count);
        Assert.False(group.TryAddBit());
        Assert.Equal(16, group.Items.Count);
    }

    [Fact]
    public void CanAddBit_TrueWhenUnder16()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();

        Assert.True(group.CanAddBit);
    }

    [Fact]
    public void CanAddBit_FalseWhenAt16()
    {
        var group = new WordBitGroup(0);
        for (var i = 0; i < 16; i++)
            group.TryAddBit();

        Assert.False(group.CanAddBit);
    }

    [Fact]
    public void TryRemoveBit_RemovesItem_AndUpdatesCount()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();
        group.TryAddBit();
        var itemToRemove = group.Items[0];

        var result = group.TryRemoveBit(itemToRemove);

        Assert.True(result);
        Assert.Single(group.Items);
        Assert.Equal(1, group.ItemCount);
    }

    [Fact]
    public void TryRemoveBit_ReturnsFalse_ForNonExistingItem()
    {
        var group = new WordBitGroup(0);
        var orphan = new BitInterpretationItem { WordIndex = 0, BitIndex = 99 };

        Assert.False(group.TryRemoveBit(orphan));
    }

    [Fact]
    public void AddExisting_AddsItemAndUpdatesCount()
    {
        var group = new WordBitGroup(0);
        var item = new BitInterpretationItem
        {
            WordIndex = 0,
            BitIndex = 5,
            Meaning = "Pump Active"
        };

        group.AddExisting(item);

        Assert.Single(group.Items);
        Assert.Equal(1, group.ItemCount);
        Assert.Equal("Pump Active", group.Items[0].Meaning);
    }

    [Fact]
    public void WordIndex_IsSettable_AndUpdatesLabel()
    {
        var group = new WordBitGroup(0);
        Assert.Equal("Word 0", group.Label);

        group.WordIndex = 3;

        Assert.Equal(3, group.WordIndex);
        Assert.Equal("Word 3", group.Label);
    }

    [Fact]
    public void HasNonEmptyMeanings_FalseWhenAllEmpty()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();
        group.TryAddBit();

        Assert.False(group.HasNonEmptyMeanings);
    }

    [Fact]
    public void HasNonEmptyMeanings_TrueWhenAnyNonEmpty()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();
        group.Items[0].Meaning = "Motor Active";

        Assert.True(group.HasNonEmptyMeanings);
    }

    [Fact]
    public void HasNonEmptyMeanings_FalseWhenWhitespaceOnly()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();
        group.Items[0].Meaning = "   ";

        Assert.False(group.HasNonEmptyMeanings);
    }

    [Fact]
    public void IsExpanded_DefaultsToTrue()
    {
        var group = new WordBitGroup(0);
        Assert.True(group.IsExpanded);
    }

    [Fact]
    public void IsExpanded_CanBeToggled()
    {
        var group = new WordBitGroup(0);

        group.IsExpanded = false;
        Assert.False(group.IsExpanded);

        group.IsExpanded = true;
        Assert.True(group.IsExpanded);
    }

    [Fact]
    public void CanRemoveBit_FalseWhenSingleBit()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();

        Assert.False(group.CanRemoveBit);
    }

    [Fact]
    public void CanRemoveBit_TrueWhenMultipleBits()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();
        group.TryAddBit();

        Assert.True(group.CanRemoveBit);
    }

    [Fact]
    public void TryRemoveLastBit_RemovesLastItem()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit(); // BitIndex 0
        group.TryAddBit(); // BitIndex 1
        group.TryAddBit(); // BitIndex 2

        var result = group.TryRemoveLastBit();

        Assert.True(result);
        Assert.Equal(2, group.Items.Count);
        Assert.Equal(1, group.Items[^1].BitIndex);
    }

    [Fact]
    public void TryRemoveLastBit_ReturnsFalse_WhenSingleBit()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();

        Assert.False(group.TryRemoveLastBit());
        Assert.Single(group.Items);
    }

    [Fact]
    public void TryRemoveLastBit_UpdatesCanRemoveBit()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();
        group.TryAddBit();
        Assert.True(group.CanRemoveBit);

        group.TryRemoveLastBit();

        Assert.False(group.CanRemoveBit);
    }
}
#endif
