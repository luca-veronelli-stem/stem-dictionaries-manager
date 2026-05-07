using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Gruppo di BitInterpretationItem per una singola word.
/// La dimensione massima è configurabile (8, 16 o 32 bit).
/// </summary>
public partial class WordBitGroup : ObservableObject
{
    /// <summary>Dimensione massima della word in bit.</summary>
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
    /// True se ci sono almeno 2 bit (rimozione possibile).
    /// </summary>
    public bool CanRemoveBit => ItemCount > 1;

    /// <summary>
    /// True se almeno un item ha un Meaning non vuoto.
    /// </summary>
    public bool HasNonEmptyMeanings => Items.Any(i => !string.IsNullOrWhiteSpace(i.Meaning));

    /// <summary>
    /// Stato espansione della word (per collapse/expand in UI).
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    public WordBitGroup(int wordIndex, int maxBitsPerWord = 16)
    {
        WordIndex = wordIndex;
        MaxBitsPerWord = maxBitsPerWord;
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

        if (nextBitIndex > MaxBitsPerWord - 1) return false;

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
    /// Rimuove l'ultimo bit della word.
    /// </summary>
    public bool TryRemoveLastBit()
    {
        if (!CanRemoveBit) return false;
        var last = Items[^1];
        Items.Remove(last);
        ItemCount = Items.Count;
        return true;
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
