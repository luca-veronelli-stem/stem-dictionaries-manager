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
            dictionary.BoardType,
            dictionary.Description,
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

    public Task<Dictionary?> GetByBoardTypeIdAsync(int boardTypeId, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByBoardTypeIdAsync:{boardTypeId}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_dictionaries.FirstOrDefault(d => d.BoardType?.Id == boardTypeId));
    }

    public Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetStandardDictionaryAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_dictionaries.FirstOrDefault(d => d.BoardType is null));
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
                dict.BoardType,
                dict.Description,
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
/// </summary>
public class MockBoardService : IBoardService
{
    private readonly List<Board> _boards = [];
    private readonly List<BoardType> _boardTypes = [];
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
            board.BoardType,
            board.Name,
            board.BoardNumber,
            board.PartNumber);
        _boards.Add(restored);
        return Task.FromResult(restored);
    }

    public Task<BoardType> AddBoardTypeAsync(BoardType boardType, CancellationToken ct = default)
    {
        MethodCalls.Add($"AddBoardTypeAsync:{boardType.Name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var restored = BoardType.Restore(_nextId++, boardType.Name, boardType.FirmwareType);
        _boardTypes.Add(restored);
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
        return Task.FromResult<IReadOnlyList<Board>>(_boards.Where(b => b.BoardType.FirmwareType == (int)deviceType).ToList());
    }

    public Task<Board?> GetByProtocolAddressAsync(uint protocolAddress, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetByProtocolAddressAsync:{protocolAddress}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<Board?>(null);
    }

    public Task<BoardType?> GetBoardTypeByNameAsync(string name, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetBoardTypeByNameAsync:{name}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_boardTypes.FirstOrDefault(bt => bt.Name == name));
    }

    public Task<BoardType?> GetBoardTypeByFirmwareTypeAsync(int firmwareType, CancellationToken ct = default)
    {
        MethodCalls.Add($"GetBoardTypeByFirmwareTypeAsync:{firmwareType}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(_boardTypes.FirstOrDefault(bt => bt.FirmwareType == firmwareType));
    }

    public Task<IReadOnlyList<BoardType>> GetBoardTypesAsync(CancellationToken ct = default)
    {
        MethodCalls.Add("GetBoardTypesAsync");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<BoardType>>(_boardTypes);
    }

    public Task UpdateAsync(Board board, CancellationToken ct = default)
    {
        MethodCalls.Add($"UpdateAsync:{board.Id}");
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        var index = _boards.FindIndex(b => b.Id == board.Id);
        if (index >= 0) _boards[index] = board;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pre-popola il mock con BoardTypes per i test.
    /// </summary>
    public void SeedBoardTypes(params BoardType[] boardTypes)
    {
        foreach (var bt in boardTypes)
        {
            var restored = BoardType.Restore(_nextId++, bt.Name, bt.FirmwareType);
            _boardTypes.Add(restored);
        }
    }

    public void Reset()
    {
        _boards.Clear();
        _boardTypes.Clear();
        _nextId = 1;
        ExceptionToThrow = null;
        MethodCalls.Clear();
    }
}
#endif
