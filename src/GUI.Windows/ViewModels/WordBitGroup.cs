using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Group of BitInterpretationItem entries belonging to a single word.
/// The maximum size is configurable (8, 16 or 32 bits).
/// </summary>
public partial class WordBitGroup : ObservableObject
{
    /// <summary>Maximum word size in bits.</summary>
    public int MaxBitsPerWord { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Label))]
    private int _wordIndex;

    public string Label => $"Word {WordIndex}";

    public ObservableCollection<BitInterpretationItem> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddBit))]
    [NotifyPropertyChangedFor(nameof(CanRemoveBit))]
    private int _itemCount;

    public bool CanAddBit => ItemCount < MaxBitsPerWord;

    /// <summary>
    /// True if there are at least 2 bits (removal allowed).
    /// </summary>
    public bool CanRemoveBit => ItemCount > 1;

    /// <summary>
    /// True if at least one item has a non-empty Meaning.
    /// </summary>
    public bool HasNonEmptyMeanings => Items.Any(i => !string.IsNullOrWhiteSpace(i.Meaning));

    /// <summary>
    /// Word expansion state (for collapse/expand in the UI).
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    public WordBitGroup(int wordIndex, int maxBitsPerWord = 16)
    {
        WordIndex = wordIndex;
        MaxBitsPerWord = maxBitsPerWord;
    }

    /// <summary>
    /// Adds a bit with an incremental BitIndex.
    /// </summary>
    public bool TryAddBit()
    {
        if (!CanAddBit)
        {
            return false;
        }

        int nextBitIndex = Items.Count > 0
            ? Items.Max(i => i.BitIndex) + 1
            : 0;

        if (nextBitIndex > MaxBitsPerWord - 1)
        {
            return false;
        }

        var item = new BitInterpretationItem
        {
            WordIndex = WordIndex,
            BitIndex = nextBitIndex
        };
        Items.Add(item);
        ItemCount = Items.Count;
        return true;
    }

    /// <summary>
    /// Removes a specific bit.
    /// </summary>
    public bool TryRemoveBit(BitInterpretationItem item)
    {
        bool removed = Items.Remove(item);
        if (removed)
        {
            ItemCount = Items.Count;
        }

        return removed;
    }

    /// <summary>
    /// Removes the last bit of the word.
    /// </summary>
    public bool TryRemoveLastBit()
    {
        if (!CanRemoveBit)
        {
            return false;
        }

        BitInterpretationItem last = Items[^1];
        Items.Remove(last);
        ItemCount = Items.Count;
        return true;
    }

    /// <summary>
    /// Adds an existing item (from the DB).
    /// </summary>
    public void AddExisting(BitInterpretationItem item)
    {
        Items.Add(item);
        ItemCount = Items.Count;
    }
}
