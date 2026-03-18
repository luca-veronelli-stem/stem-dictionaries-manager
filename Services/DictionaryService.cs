using Core.Models;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione dizionari (aggregate root).
/// Include operazioni su Variables tramite Dictionary.
/// </summary>
public class DictionaryService : IDictionaryService
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IVariableRepository _variableRepository;
    private readonly IBoardTypeRepository _boardTypeRepository;

    public DictionaryService(
        IDictionaryRepository dictionaryRepository,
        IVariableRepository variableRepository,
        IBoardTypeRepository boardTypeRepository)
    {
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(variableRepository);
        ArgumentNullException.ThrowIfNull(boardTypeRepository);

        _dictionaryRepository = dictionaryRepository;
        _variableRepository = variableRepository;
        _boardTypeRepository = boardTypeRepository;
    }

    // === CRUD Base ===

    public async Task<Dictionary?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _dictionaryRepository.GetByIdAsync(id, ct);
        if (entity is null)
            return null;
        
        BoardType? boardType = null;
        if (entity.BoardTypeId.HasValue)
        {
            var boardTypeEntity = await _boardTypeRepository.GetByIdAsync(entity.BoardTypeId.Value, ct);
            if (boardTypeEntity is not null)
                boardType = BoardTypeMapper.ToDomain(boardTypeEntity);
        }
        
        return DictionaryMapper.ToDomain(entity, boardType);
    }

    public async Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _dictionaryRepository.GetAllWithBoardTypeAsync(ct);

        return [.. entities.Select(e => 
        {
            BoardType? boardType = e.BoardType is not null 
                ? BoardTypeMapper.ToDomain(e.BoardType) 
                : null;
            return DictionaryMapper.ToDomain(e, boardType);
        })];
    }

    public async Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        
        // Verifica unicità nome
        var existingByName = await _dictionaryRepository.GetByNameAsync(dictionary.Name, ct);
        if (existingByName is not null)
            throw new InvalidOperationException($"Dictionary with name '{dictionary.Name}' already exists.");
        
        // Verifica BoardType se specificato
        if (dictionary.BoardType is not null)
        {
            _ = await _boardTypeRepository.GetByIdAsync(dictionary.BoardType.Id, ct) 
                ?? throw new InvalidOperationException($"BoardType with Id {dictionary.BoardType.Id} not found.");

            // Verifica che BoardType non abbia già un dizionario (BR-002)
            var existingByBoardType = await _dictionaryRepository.GetByBoardTypeAsync(dictionary.BoardType.Id, ct);
            if (existingByBoardType is not null)
                throw new InvalidOperationException(
                    $"BoardType {dictionary.BoardType.Id} already has a dictionary assigned.");
        }
        
        var entity = DictionaryMapper.ToEntity(dictionary);
        var created = await _dictionaryRepository.AddAsync(entity, ct);
        
        return await GetByIdAsync(created.Id, ct) 
            ?? throw new InvalidOperationException("Failed to retrieve created dictionary.");
    }

    public async Task UpdateAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        
        var entity = await _dictionaryRepository.GetByIdAsync(dictionary.Id, ct)
            ?? throw new KeyNotFoundException($"Dictionary with Id {dictionary.Id} not found.");
        
        // Verifica unicità nome (se cambiato)
        if (!entity.Name.Equals(dictionary.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existingByName = await _dictionaryRepository.GetByNameAsync(dictionary.Name, ct);
            if (existingByName is not null)
                throw new InvalidOperationException($"Dictionary with name '{dictionary.Name}' already exists.");
        }
        
        DictionaryMapper.UpdateEntity(entity, dictionary);
        await _dictionaryRepository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _dictionaryRepository.DeleteAsync(id, ct);
    }

    // === Query Specifiche ===

    public async Task<Dictionary?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var entity = await _dictionaryRepository.GetByNameAsync(name, ct);
        if (entity is null)
            return null;
        
        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Dictionary?> GetByBoardTypeIdAsync(int boardTypeId, CancellationToken ct = default)
    {
        var entity = await _dictionaryRepository.GetByBoardTypeAsync(boardTypeId, ct);
        if (entity is null)
            return null;
        
        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default)
    {
        // Il dizionario "Standard" non ha BoardType (BoardTypeId = null)
        var entity = await _dictionaryRepository.GetStandardDictionaryAsync(ct);

        if (entity is null)
            return null;

        return DictionaryMapper.ToDomain(entity);
    }

    // === Aggregate Operations ===

    public async Task<Dictionary?> GetWithVariablesAsync(int id, CancellationToken ct = default)
    {
        var entity = await _dictionaryRepository.GetWithVariablesAsync(id, ct);
        if (entity is null)
            return null;
        
        BoardType? boardType = null;
        if (entity.BoardTypeId.HasValue)
        {
            var boardTypeEntity = await _boardTypeRepository.GetByIdAsync(entity.BoardTypeId.Value, ct);
            if (boardTypeEntity is not null)
                boardType = BoardTypeMapper.ToDomain(boardTypeEntity);
        }
        
        return DictionaryMapper.ToDomainWithVariables(entity, boardType);
    }

    public async Task<Variable> AddVariableAsync(int dictionaryId, Variable variable, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);
        
        // Verifica che il dizionario esista
        var dictionary = await _dictionaryRepository.GetWithVariablesAsync(dictionaryId, ct)
            ?? throw new KeyNotFoundException($"Dictionary with Id {dictionaryId} not found.");

        // Verifica unicità indirizzo (usa logica del Domain Model)
        _ = DictionaryMapper.ToDomainWithVariables(dictionary);

        // Il Domain Model Dictionary.AddVariable già valida l'unicità,
        // ma dobbiamo verificare prima per dare un messaggio chiaro
        var existingByAddress = await _variableRepository.GetByAddressAsync(
            dictionaryId, variable.AddressHigh, variable.AddressLow, ct);
        if (existingByAddress is not null)
            throw new InvalidOperationException(
                $"Variable with address 0x{variable.AddressHigh:X2}{variable.AddressLow:X2} " +
                $"already exists in dictionary '{dictionary.Name}'.");
        
        var entity = VariableMapper.ToEntity(variable, dictionaryId);
        var created = await _variableRepository.AddAsync(entity, ct);
        return VariableMapper.ToDomain(created);
    }

    public async Task RemoveVariableAsync(int dictionaryId, int variableId, CancellationToken ct = default)
    {
        // Verifica che il dizionario esista
        var dictionaryExists = await _dictionaryRepository.ExistsAsync(dictionaryId, ct);
        if (!dictionaryExists)
            throw new KeyNotFoundException($"Dictionary with Id {dictionaryId} not found.");
        
        // Verifica che la variabile appartenga al dizionario
        var variable = await _variableRepository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException($"Variable with Id {variableId} not found.");
        
        if (variable.DictionaryId != dictionaryId)
            throw new InvalidOperationException(
                $"Variable {variableId} does not belong to dictionary {dictionaryId}.");
        
        await _variableRepository.DeleteAsync(variableId, ct);
    }
}
