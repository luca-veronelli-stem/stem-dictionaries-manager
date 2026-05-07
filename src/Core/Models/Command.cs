namespace Core.Models;

/// <summary>
/// Comando del protocollo STEM (universale per tutti i device).
/// </summary>
public class Command
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public byte CodeHigh { get; private set; }
    public byte CodeLow { get; private set; }
    public bool IsResponse { get; private set; }

    private readonly List<string> _parameters = [];
    public IReadOnlyList<string> Parameters => _parameters.AsReadOnly();

    /// <summary>
    /// Full command code composed as <c>(CodeHigh &lt;&lt; 8) | CodeLow</c>.
    /// </summary>
    public ushort FullCode => (ushort)((CodeHigh << 8) | CodeLow);

    public Command(string name, byte codeHigh, byte codeLow, bool isResponse = false,
        IEnumerable<string>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        CodeHigh = codeHigh;
        CodeLow = codeLow;
        IsResponse = isResponse;

        if (parameters != null)
        {
            _parameters.AddRange(parameters);
        }
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Command Restore(int id, string name, byte codeHigh, byte codeLow,
        bool isResponse, IEnumerable<string> parameters)
    {
        var command = new Command(name, codeHigh, codeLow, isResponse, parameters)
        {
            Id = id
        };
        return command;
    }
}
