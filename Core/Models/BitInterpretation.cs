namespace Core.Models;

/// <summary>
/// Interpretazione di un bit per variabili bitmapped.
/// SESSION_037: DeviceId? aggiunto per override per-device.
/// DeviceId = null ? interpretazione comune a tutti i device.
/// DeviceId valorizzato ? override per device specifico (BR-018: priorità su comune).
/// </summary>
public class BitInterpretation
{
    public int Id { get; private set; }
    public int VariableId { get; private set; }
    public int? DeviceId { get; private set; }
    public int WordIndex { get; private set; }
    public int BitIndex { get; private set; }
    public string? Meaning { get; private set; }

    public BitInterpretation(int variableId, int wordIndex, int bitIndex,
        string? meaning, int? deviceId, int maxBitIndex = 15)
    {
        if (wordIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(wordIndex), "WordIndex must be non-negative.");
        if (bitIndex < 0 || bitIndex > maxBitIndex)
            throw new ArgumentOutOfRangeException(nameof(bitIndex),
                $"BitIndex must be between 0 and {maxBitIndex}.");

        VariableId = variableId;
        DeviceId = deviceId;
        WordIndex = wordIndex;
        BitIndex = bitIndex;
        Meaning = meaning;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static BitInterpretation Restore(int id, int variableId,
        int wordIndex, int bitIndex, string? meaning, int? deviceId)
    {
        var interpretation = new BitInterpretation(variableId, wordIndex, bitIndex, meaning, deviceId)
        {
            Id = id
        };
        return interpretation;
    }
}
