namespace Core.Models;

/// <summary>
/// STEM protocol command (universal across all devices).
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
    /// Factory method to reconstruct from the DB.
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

    /// <summary>
    /// Updates the command name.
    /// </summary>
    public void UpdateName(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName;
    }
}
