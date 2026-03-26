#if WINDOWS
using Core.Enums;
using Core.Models;
using Services.Interfaces;

namespace Tests.Unit.GUI.Mocks;

/// <summary>
/// Mock implementation di IDictionaryService per i test dei ViewModels.
/// </summary>
public class MockDictionaryService : IDictionaryService
{
    private readonly List<Dictionary> _dictionaries = [];
    private int _nextId = 1;

    /// <summary>
    /// Exception da lanciare nei metodi (per testare error handling).
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// Traccia le chiamate ai metodi.
    /// </summary>
    public List<string> MethodCalls { get; } = [];

    public Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddAsync:{dictionary.Name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var restored = Dictionary.Restore(
            _nextId++,
            dictionary.Name,
            dictionary.Description,
            dictionary.IsStandard,
            dictionary.Variables);
        _dictionaries.Add(restored);
        return Task.FromResult(restored);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"DeleteAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var dict = _dictionaries.FirstOrDefault(d => d.Id == id);
        if (dict is not null) _dictionaries.Remove(dict);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetAllAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<Dictionary>>(_dictionaries);
    }

    public Task<Dictionary?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByIdAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_dictionaries.FirstOrDefault(d => d.Id == id));
    }

    public Task<Dictionary?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByNameAsync:{name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_dictionaries.FirstOrDefault(d => d.Name == name));
    }

    public Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetStandardDictionaryAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_dictionaries.FirstOrDefault(d => d.IsStandard));
    }

    public Task<Dictionary?> GetWithVariablesAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetWithVariablesAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_dictionaries.FirstOrDefault(d => d.Id == id));
    }

    public Task UpdateAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        MethodCalls.Add($"UpdateAsync:{dictionary.Id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var index = _dictionaries.FindIndex(d => d.Id == dictionary.Id);
        if (index >= 0) _dictionaries[index] = dictionary;
        return Task.CompletedTask;
    }

    public Task<Variable> AddVariableAsync(int dictionaryId, Variable variable, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddVariableAsync:{dictionaryId}:{variable.Name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(variable);
    }

    public Task RemoveVariableAsync(int dictionaryId, int variableId, CancellationToken ct = default)
    {
        MethodCalls.Add($"RemoveVariableAsync:{dictionaryId}:{variableId}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pre-popola il mock con dizionari per i test.
    /// </summary>
    public void SeedData(params Dictionary[] dictionaries)
    {
        foreach (var dict in dictionaries)
        {
            var restored = Dictionary.Restore(
                _nextId++,
                dict.Name,
                dict.Description,
                dict.IsStandard,
                dict.Variables);
            _dictionaries.Add(restored);
        }
    }

    public void Reset()
    {
        _dictionaries.Clear();
        _nextId = 1;
        ExceptionToThrow = null;
        MethodCalls.Clear();
    }
}

/// <summary>
/// Mock implementation di IBoardService per i test dei ViewModels.
/// Domain v2: nessun BoardType.
/// </summary>
public class MockBoardService : IBoardService
{
    private readonly List<Board> _boards = [];
    private int _nextId = 1;

    public Exception? ExceptionToThrow { get; set; }
    public List<string> MethodCalls { get; } = [];

    public Task<Board> AddAsync(Board board, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddAsync:{board.Name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var restored = Board.Restore(
            _nextId++,
            board.DeviceType,
            board.Name,
            board.FirmwareType,
            board.BoardNumber,
            board.PartNumber,
            board.IsPrimary,
            board.DictionaryId);
        _boards.Add(restored);
        return Task.FromResult(restored);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"DeleteAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var board = _boards.FirstOrDefault(b => b.Id == id);
        if (board is not null) _boards.Remove(board);
        return Task.CompletedTask;
    }

    public Task<Board?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByIdAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_boards.FirstOrDefault(b => b.Id == id));
    }

    public Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetAllAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<Board>>(_boards);
    }

    public Task<IReadOnlyList<Board>> GetByDeviceTypeAsync(DeviceType deviceType, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByDeviceTypeAsync:{deviceType}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<Board>>([.. _boards.Where(b => b.DeviceType == deviceType)]);
    }

    public Task<Board?> GetByProtocolAddressAsync(uint protocolAddress, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByProtocolAddressAsync:{protocolAddress}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<Board?>(null);
    }

    public Task UpdateAsync(Board board, CancellationToken ct = default)
    {
        MethodCalls.Add($"UpdateAsync:{board.Id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var index = _boards.FindIndex(b => b.Id == board.Id);
        if (index >= 0) _boards[index] = board;
        return Task.CompletedTask;
    }

    public void SeedBoards(params Board[] boards)
    {
        foreach (var b in boards)
        {
            var restored = Board.Restore(
                _nextId++, b.DeviceType, b.Name, b.FirmwareType,
                b.BoardNumber, b.PartNumber, b.IsPrimary, b.DictionaryId);
            _boards.Add(restored);
        }
    }

    public void Reset()
    {
        _boards.Clear();
        _nextId = 1;
        ExceptionToThrow = null;
        MethodCalls.Clear();
    }
}

/// <summary>
/// Mock implementation di IVariableService per i test dei ViewModels.
/// </summary>
public class MockVariableService : IVariableService
{
    private readonly List<Variable> _variables = [];
    private readonly Dictionary<int, List<BitInterpretation>> _bitInterpretations = [];
    private int _nextId = 1;

    public Exception? ExceptionToThrow { get; set; }
    public List<string> MethodCalls { get; } = [];

    public Task<Variable> AddAsync(int dictionaryId, Variable variable, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddAsync:{dictionaryId}:{variable.Name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var restored = Variable.Restore(
            _nextId++,
            variable.Name,
            variable.AddressHigh,
            variable.AddressLow,
            variable.DataTypeKind,
            variable.DataTypeRaw,
            variable.DataTypeParam,
            variable.AccessMode,
            variable.IsEnabled,
            variable.Format,
            variable.MinValue,
            variable.MaxValue,
            variable.Unit,
            variable.Usage,
            variable.Description);
        _variables.Add(restored);
        return Task.FromResult(restored);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"DeleteAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var variable = _variables.FirstOrDefault(v => v.Id == id);
        if (variable is not null) _variables.Remove(variable);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Variable>> GetAllAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetAllAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<Variable>>(_variables);
    }

    public Task<Variable?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByIdAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_variables.FirstOrDefault(v => v.Id == id));
    }

    public Task<IReadOnlyList<Variable>> GetByDictionaryIdAsync(int dictionaryId, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByDictionaryIdAsync:{dictionaryId}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<Variable>>(_variables);
    }

    public Task<Variable?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByAddressAsync:{dictionaryId}:0x{addressHigh:X2}{addressLow:X2}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_variables.FirstOrDefault(v => v.AddressHigh == addressHigh && v.AddressLow == addressLow));
    }

    public Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsAsync(int variableId, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetBitInterpretationsAsync:{variableId}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        if (_bitInterpretations.TryGetValue(variableId, out var bits))
            return Task.FromResult<IReadOnlyList<BitInterpretation>>(bits);
        return Task.FromResult<IReadOnlyList<BitInterpretation>>([]);
    }

    public Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddBitInterpretationAsync:{variableId}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(interpretation);
    }

    public Task UpdateBitInterpretationsAsync(int variableId, IEnumerable<BitInterpretation> interpretations, CancellationToken ct = default)
    {
        MethodCalls.Add($"UpdateBitInterpretationsAsync:{variableId}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    public Task SetDeviceStateAsync(int variableId, DeviceType deviceType, bool isEnabled, CancellationToken ct = default)
    {
        MethodCalls.Add($"SetDeviceStateAsync:{variableId}:{deviceType}:{isEnabled}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    public Task<VariableDeviceState?> GetDeviceStateAsync(int variableId, DeviceType deviceType, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetDeviceStateAsync:{variableId}:{deviceType}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<VariableDeviceState?>(null);
    }

    public Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesAsync(int variableId, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetDeviceStatesAsync:{variableId}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<VariableDeviceState>>([]);
    }

    public Task UpdateAsync(Variable variable, CancellationToken ct = default)
    {
        MethodCalls.Add($"UpdateAsync:{variable.Id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var index = _variables.FindIndex(v => v.Id == variable.Id);
        if (index >= 0) _variables[index] = variable;
        return Task.CompletedTask;
    }

    public void SeedData(params Variable[] variables)
    {
        foreach (var v in variables)
        {
            var restored = Variable.Restore(
                _nextId++,
                v.Name,
                v.AddressHigh,
                v.AddressLow,
                v.DataTypeKind,
                v.DataTypeRaw,
                v.DataTypeParam,
                v.AccessMode,
                v.IsEnabled,
                v.Format,
                v.MinValue,
                v.MaxValue,
                v.Unit,
                v.Usage,
                v.Description);
            _variables.Add(restored);
        }
    }

    public void SeedBitInterpretations(int variableId, List<BitInterpretation> bits)
    {
        _bitInterpretations[variableId] = bits;
    }

    public void Reset()
    {
        _variables.Clear();
        _bitInterpretations.Clear();
        _nextId = 1;
        ExceptionToThrow = null;
        MethodCalls.Clear();
    }
}

/// <summary>
/// Mock implementation di ICommandService per i test dei ViewModels.
/// </summary>
public class MockCommandService : ICommandService
{
    private readonly List<Command> _commands = [];
    private int _nextId = 1;

    public Exception? ExceptionToThrow { get; set; }
    public List<string> MethodCalls { get; } = [];

    public Task<Command> AddAsync(Command command, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddAsync:{command.Name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var restored = Command.Restore(
            _nextId++,
            command.Name,
            command.CodeHigh,
            command.CodeLow,
            command.IsResponse,
            command.Parameters);
        _commands.Add(restored);
        return Task.FromResult(restored);
    }

    /// <summary>
    /// Restituisce l'ultimo comando salvato (per verifiche test).
    /// </summary>
    public Command? GetSavedCommand() => _commands.LastOrDefault();

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"DeleteAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var command = _commands.FirstOrDefault(c => c.Id == id);
        if (command is not null) _commands.Remove(command);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetAllAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<Command>>(_commands);
    }

    public Task<Command?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByIdAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_commands.FirstOrDefault(c => c.Id == id));
    }

    public Task<Command?> GetByCodeAsync(byte codeHigh, byte codeLow, bool isResponse, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByCodeAsync:0x{codeHigh:X2}{codeLow:X2}:{isResponse}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_commands.FirstOrDefault(c =>
            c.CodeHigh == codeHigh && c.CodeLow == codeLow && c.IsResponse == isResponse));
    }

    public Task<Command?> GetWithDeviceStatesAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetWithDeviceStatesAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_commands.FirstOrDefault(c => c.Id == id));
    }

    public Task SetDeviceStateAsync(int commandId, DeviceType deviceType, bool isEnabled, CancellationToken ct = default)
    {
        MethodCalls.Add($"SetDeviceStateAsync:{commandId}:{deviceType}:{isEnabled}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    public Task<CommandDeviceState?> GetDeviceStateAsync(int commandId, DeviceType deviceType, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetDeviceStateAsync:{commandId}:{deviceType}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<CommandDeviceState?>(null);
    }

    public Task UpdateAsync(Command command, CancellationToken ct = default)
    {
        MethodCalls.Add($"UpdateAsync:{command.Id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var index = _commands.FindIndex(c => c.Id == command.Id);
        if (index >= 0) _commands[index] = command;
        return Task.CompletedTask;
    }

    public void SeedData(params Command[] commands)
    {
        foreach (var c in commands)
        {
            var restored = Command.Restore(
                _nextId++,
                c.Name,
                c.CodeHigh,
                c.CodeLow,
                c.IsResponse,
                c.Parameters);
            _commands.Add(restored);
        }
    }

    public void Reset()
    {
        _commands.Clear();
        _nextId = 1;
        ExceptionToThrow = null;
        MethodCalls.Clear();
    }
}

/// <summary>
/// Mock implementation di IUserService per i test dei ViewModels.
/// </summary>
public class MockUserService : IUserService
{
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public Exception? ExceptionToThrow { get; set; }
    public List<string> MethodCalls { get; } = [];

    public Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddAsync:{user.Username}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var restored = User.Restore(_nextId++, user.Username, user.DisplayName);
        _users.Add(restored);
        return Task.FromResult(restored);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"DeleteAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user is not null) _users.Remove(user);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetAllAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<User>>(_users);
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByIdAsync:{id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByUsernameAsync:{username}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        MethodCalls.Add($"UsernameExistsAsync:{username}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_users.Any(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        MethodCalls.Add($"UpdateAsync:{user.Id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index >= 0) _users[index] = user;
        return Task.CompletedTask;
    }

    public void SeedData(params User[] users)
    {
        foreach (var u in users)
        {
            var restored = User.Restore(_nextId++, u.Username, u.DisplayName);
            _users.Add(restored);
        }
    }

    public void Reset()
    {
        _users.Clear();
        _nextId = 1;
        ExceptionToThrow = null;
        MethodCalls.Clear();
    }
}
#endif
