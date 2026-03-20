namespace Core.Models;

/// <summary>
/// Interpretazione di un bit per variabili bitmapped.
/// Il DeviceType è inferito dalla catena Variable ? Dictionary ? DeviceType.
/// </summary>
public class BitInterpretation
{
    public int Id { get; private set; }
    public int VariableId { get; private set; }
    public int WordIndex { get; private set; }
    public int BitIndex { get; private set; }
    public string? Meaning { get; private set; }

    public BitInterpretation(int variableId, int wordIndex, int bitIndex, string? meaning)
    {
        if (wordIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(wordIndex), "WordIndex must be non-negative.");
        if (bitIndex < 0 || bitIndex > 15)
            throw new ArgumentOutOfRangeException(nameof(bitIndex), "BitIndex must be between 0 and 15.");

        VariableId = variableId;
        WordIndex = wordIndex;
        BitIndex = bitIndex;
        Meaning = meaning;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static BitInterpretation Restore(int id, int variableId,
        int wordIndex, int bitIndex, string? meaning)
    {
        var interpretation = new BitInterpretation(variableId, wordIndex, bitIndex, meaning)
        {
            Id = id
        };
        return interpretation;
    }
}
