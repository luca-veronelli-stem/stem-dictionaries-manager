using Core.Models;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Service per gestione dizionari (aggregate root).
/// Domain v2: IsStandard flag, nessun BoardType/DeviceType.
/// </summary>
public class DictionaryService : IDictionaryService
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IVariableRepository _variableRepository;

    public DictionaryService(
        IDictionaryRepository dictionaryRepository,
        IVariableRepository variableRepository)
    {
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(variableRepository);
        _dictionaryRepository = dictionaryRepository;
        _variableRepository = variableRepository;
    }

    public async Task<Dictionary?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _dictionaryRepository.GetByIdAsync(id, ct);
        return entity is null ? null : DictionaryMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _dictionaryRepository.GetAllWithVariablesAsync(ct);
        return [.. entities.Select(DictionaryMapper.ToDomainWithVariables)];
    }

    public async Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        // Verifica unicità nome
        var existingByName = await _dictionaryRepository.GetByNameAsync(dictionary.Name, ct);
        if (existingByName is not null)
            throw new InvalidOperationException(
                $"Dictionary with name '{dictionary.Name}' already exists.");

        // BR-004: max 1 dizionario Standard
        if (dictionary.IsStandard)
        {
            var existingStandard = await _dictionaryRepository.GetStandardDictionaryAsync(ct);
            if (existingStandard is not null)
                throw new InvalidOperationException(
                    "A Standard dictionary already exists. Only one is allowed (BR-004).");
        }

        var entity = DictionaryMapper.ToEntity(dictionary);
        var created = await _dictionaryRepository.AddAsync(entity, ct);

        return DictionaryMapper.ToDomain(created);
    }

    public async Task UpdateAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        var entity = await _dictionaryRepository.GetByIdAsync(dictionary.Id, ct)
            ?? throw new KeyNotFoundException(
                $"Dictionary '{dictionary.Name}' (Id={dictionary.Id}) not found.");

        // Verifica unicità nome (se cambiato)
        if (!entity.Name.Equals(dictionary.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existingByName = await _dictionaryRepository.GetByNameAsync(dictionary.Name, ct);
            if (existingByName is not null)
                throw new InvalidOperationException(
                    $"Dictionary with name '{dictionary.Name}' already exists.");
        }

        // BR-004: se diventa Standard, verifica che non ne esista già uno
        if (dictionary.IsStandard && !entity.IsStandard)
        {
            var existingStandard = await _dictionaryRepository.GetStandardDictionaryAsync(ct);
            if (existingStandard is not null)
                throw new InvalidOperationException(
                    "A Standard dictionary already exists. Only one is allowed (BR-004).");
        }

        DictionaryMapper.UpdateEntity(entity, dictionary);
        await _dictionaryRepository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _dictionaryRepository.DeleteAsync(id, ct);
    }

    public async Task<Dictionary?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var entity = await _dictionaryRepository.GetByNameAsync(name, ct);
        return entity is null ? null : DictionaryMapper.ToDomain(entity);
    }

    public async Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default)
    {
        var entity = await _dictionaryRepository.GetStandardDictionaryAsync(ct);
        return entity is null ? null : DictionaryMapper.ToDomainWithVariables(entity);
    }

    public async Task<Dictionary?> GetWithVariablesAsync(int id, CancellationToken ct = default)
    {
        var entity = await _dictionaryRepository.GetWithVariablesAsync(id, ct);
        return entity is null ? null : DictionaryMapper.ToDomainWithVariables(entity);
    }

    public async Task<Variable> AddVariableAsync(int dictionaryId, Variable variable,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        var dictionary = await _dictionaryRepository.GetWithVariablesAsync(dictionaryId, ct)
            ?? throw new KeyNotFoundException(
                $"Dictionary (Id={dictionaryId}) not found.");

        // Verifica unicità indirizzo
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

    public async Task RemoveVariableAsync(int dictionaryId, int variableId,
        CancellationToken ct = default)
    {
        if (!await _dictionaryRepository.ExistsAsync(dictionaryId, ct))
            throw new KeyNotFoundException(
                $"Dictionary (Id={dictionaryId}) not found.");

        var variable = await _variableRepository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException(
                $"Variable (Id={variableId}) not found.");

        if (variable.DictionaryId != dictionaryId)
            throw new InvalidOperationException(
                $"Variable '{variable.Name}' does not belong to this dictionary.");

        await _variableRepository.DeleteAsync(variableId, ct);
    }
}
