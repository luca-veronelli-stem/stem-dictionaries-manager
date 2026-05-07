namespace Core.Models;

/// <summary>
/// Bit interpretation for bitmapped variables.
/// v7: DictionaryId? replaces DeviceId? (per-dictionary scope).
/// DictionaryId = null ? defined in the Standard template (default/fallback).
/// DictionaryId set ? override for a specific dictionary (BR-018: takes priority over the template).
/// </summary>
public class BitInterpretation
{
    public int Id { get; private set; }
    public int VariableId { get; private set; }
    public int? DictionaryId { get; private set; }
    public int WordIndex { get; private set; }
    public int BitIndex { get; private set; }
    public string? Meaning { get; private set; }

    public BitInterpretation(int variableId, int wordIndex, int bitIndex,
        string? meaning, int? dictionaryId, int maxBitIndex = 15)
    {
        if (wordIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wordIndex), "WordIndex must be non-negative.");
        }

        if (bitIndex < 0 || bitIndex > maxBitIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(bitIndex),
                $"BitIndex must be between 0 and {maxBitIndex}.");
        }

        VariableId = variableId;
        DictionaryId = dictionaryId;
        WordIndex = wordIndex;
        BitIndex = bitIndex;
        Meaning = meaning;
    }

    /// <summary>
    /// Factory method to reconstruct from the DB.
    /// </summary>
    public static BitInterpretation Restore(int id, int variableId,
        int wordIndex, int bitIndex, string? meaning, int? dictionaryId)
    {
        var interpretation = new BitInterpretation(variableId, wordIndex, bitIndex, meaning, dictionaryId)
        {
            Id = id
        };
        return interpretation;
    }
}
