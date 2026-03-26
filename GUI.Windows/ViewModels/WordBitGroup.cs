using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Gruppo di BitInterpretationItem per una singola word (max 16 bit).
/// </summary>
public partial class WordBitGroup : ObservableObject
{
    private const int MaxBitsPerWord = 16;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Label))]
    private int _wordIndex;

    public string Label => $"Word {WordIndex}";

    public ObservableCollection<BitInterpretationItem> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddBit))]
    private int _itemCount;

    public bool CanAddBit => ItemCount < MaxBitsPerWord;

    /// <summary>
    /// True se almeno un item ha un Meaning non vuoto.
    /// </summary>
    public bool HasNonEmptyMeanings => Items.Any(i => !string.IsNullOrWhiteSpace(i.Meaning));

    public WordBitGroup(int wordIndex)
    {
        WordIndex = wordIndex;
    }

    /// <summary>
    /// Aggiunge un bit con BitIndex incrementale.
    /// </summary>
    public bool TryAddBit()
    {
        if (!CanAddBit) return false;

        var nextBitIndex = Items.Count > 0
            ? Items.Max(i => i.BitIndex) + 1
            : 0;

        if (nextBitIndex > 15) return false;

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
    /// Rimuove un bit specifico.
    /// </summary>
    public bool TryRemoveBit(BitInterpretationItem item)
    {
        var removed = Items.Remove(item);
        if (removed) ItemCount = Items.Count;
        return removed;
    }

    /// <summary>
    /// Aggiunge un item esistente (da DB).
    /// </summary>
    public void AddExisting(BitInterpretationItem item)
    {
        Items.Add(item);
        ItemCount = Items.Count;
    }
}
