namespace Core.Models;

/// <summary>
/// Interpretazione di un bit per variabili bitmapped.
/// v7: DictionaryId? sostituisce DeviceId? (scope per-dizionario).
/// DictionaryId = null ? definita nel template Standard (default/fallback).
/// DictionaryId valorizzato ? override per dizionario specifico (BR-018: priorità su template).
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
            throw new ArgumentOutOfRangeException(nameof(wordIndex), "WordIndex must be non-negative.");
        if (bitIndex < 0 || bitIndex > maxBitIndex)
            throw new ArgumentOutOfRangeException(nameof(bitIndex),
                $"BitIndex must be between 0 and {maxBitIndex}.");

        VariableId = variableId;
        DictionaryId = dictionaryId;
        WordIndex = wordIndex;
        BitIndex = bitIndex;
        Meaning = meaning;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
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
