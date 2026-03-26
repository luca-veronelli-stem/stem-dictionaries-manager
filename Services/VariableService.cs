using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione variabili.
/// Per operazioni aggregate su Dictionary, usare DictionaryService.
/// </summary>
public class VariableService : IVariableService
{
    private readonly IVariableRepository _repository;
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IBitInterpretationRepository _bitInterpretationRepository;
    private readonly IVariableDeviceStateRepository _deviceStateRepository;

    public VariableService(
        IVariableRepository repository,
        IDictionaryRepository dictionaryRepository,
        IBitInterpretationRepository bitInterpretationRepository,
        IVariableDeviceStateRepository deviceStateRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(bitInterpretationRepository);
        ArgumentNullException.ThrowIfNull(deviceStateRepository);
        _repository = repository;
        _dictionaryRepository = dictionaryRepository;
        _bitInterpretationRepository = bitInterpretationRepository;
        _deviceStateRepository = deviceStateRepository;
    }

    // === CRUD Base ===

    public async Task<Variable?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : VariableMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Variable>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetAllAsync(ct);
        return VariableMapper.ToDomainList(entities);
    }

    public async Task<Variable> AddAsync(int dictionaryId, Variable variable, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        // Verifica che il dizionario esista
        var dictionaryExists = await _dictionaryRepository.ExistsAsync(dictionaryId, ct);
        if (!dictionaryExists)
            throw new KeyNotFoundException($"Dictionary with Id {dictionaryId} not found.");

        // Verifica unicità indirizzo nel dizionario
        var existingByAddress = await _repository.GetByAddressAsync(
            dictionaryId, variable.AddressHigh, variable.AddressLow, ct);
        if (existingByAddress is not null)
            throw new InvalidOperationException(
                $"Variable with address 0x{variable.AddressHigh:X2}{variable.AddressLow:X2} " +
                $"already exists in dictionary {dictionaryId}.");

        var entity = VariableMapper.ToEntity(variable, dictionaryId);
        var created = await _repository.AddAsync(entity, ct);
        return VariableMapper.ToDomain(created);
    }

    public async Task UpdateAsync(Variable variable, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        var entity = await _repository.GetByIdAsync(variable.Id, ct)
            ?? throw new KeyNotFoundException($"Variable with Id {variable.Id} not found.");

        // Verifica unicità indirizzo (se cambiato)
        if (entity.AddressHigh != variable.AddressHigh || entity.AddressLow != variable.AddressLow)
        {
            var existingByAddress = await _repository.GetByAddressAsync(
                entity.DictionaryId, variable.AddressHigh, variable.AddressLow, ct);
            if (existingByAddress is not null && existingByAddress.Id != variable.Id)
                throw new InvalidOperationException(
                    $"Variable with address 0x{variable.AddressHigh:X2}{variable.AddressLow:X2} " +
                    $"already exists in this dictionary.");
        }

        VariableMapper.UpdateEntity(entity, variable);
        await _repository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
    }

    // === Query Specifiche ===

    public async Task<IReadOnlyList<Variable>> GetByDictionaryIdAsync(int dictionaryId, CancellationToken ct = default)
    {
        var entities = await _repository.GetByDictionaryIdAsync(dictionaryId, ct);
        return VariableMapper.ToDomainList(entities);
    }

    public async Task<Variable?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow,
        CancellationToken ct = default)
    {
        var entity = await _repository.GetByAddressAsync(dictionaryId, addressHigh, addressLow, ct);
        return entity is null ? null : VariableMapper.ToDomain(entity);
    }

    // === BitInterpretation Management ===

    public async Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsAsync(int variableId,
        CancellationToken ct = default)
    {
        // Verifica che la variabile esista
        var variableExists = await _repository.ExistsAsync(variableId, ct);
        if (!variableExists)
            throw new KeyNotFoundException($"Variable with Id {variableId} not found.");

        var entities = await _bitInterpretationRepository.GetByVariableIdAsync(variableId, ct);

        return BitInterpretationMapper.ToDomainList(entities);
    }

    public async Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(interpretation);

        // Verifica che la variabile esista ed sia bitmapped
        var variable = await _repository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException($"Variable with Id {variableId} not found.");

        if (variable.DataTypeKind != DataTypeKind.Bitmapped)
            throw new InvalidOperationException(
                $"Variable {variableId} is not bitmapped. Cannot add bit interpretation.");

        var entity = BitInterpretationMapper.ToEntity(interpretation);
        entity.VariableId = variableId;

        var created = await _bitInterpretationRepository.AddAsync(entity, ct);

        return BitInterpretationMapper.ToDomain(created);
    }

    public async Task UpdateBitInterpretationsAsync(int variableId,
        IEnumerable<BitInterpretation> interpretations, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(interpretations);

        var variable = await _repository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException($"Variable with Id {variableId} not found.");

        if (variable.DataTypeKind != DataTypeKind.Bitmapped)
            throw new InvalidOperationException(
                $"Variable {variableId} is not bitmapped. Cannot update bit interpretations.");

        var incoming = interpretations.ToList();

        // Validazione: nessun duplicato (WordIndex, BitIndex)
        var keys = incoming.Select(i => (i.WordIndex, i.BitIndex)).ToList();
        if (keys.Distinct().Count() != keys.Count)
            throw new InvalidOperationException(
                "Duplicate (WordIndex, BitIndex) found in incoming interpretations.");

        // Validazione: BitIndex 0-15, WordIndex >= 0
        foreach (var i in incoming)
        {
            if (i.BitIndex is < 0 or > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(interpretations), $"BitIndex must be between 0 and 15, got {i.BitIndex}.");
            if (i.WordIndex < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(interpretations), $"WordIndex must be non-negative, got {i.WordIndex}.");
        }

        var entities = incoming.Select(BitInterpretationMapper.ToEntity).ToList();
        await _bitInterpretationRepository.SyncByVariableIdAsync(variableId, entities, ct);
    }

    // === DeviceState Management ===

    public async Task SetDeviceStateAsync(int variableId, DeviceType deviceType, bool isEnabled,
        CancellationToken ct = default)
    {
        // Verifica che la variabile esista
        var variable = await _repository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException($"Variable with Id {variableId} not found.");

        // BR-011: override isEnabled=true vietato se Variable.IsEnabled=false
        if (!variable.IsEnabled && isEnabled)
            throw new InvalidOperationException(
                $"Cannot enable variable {variableId} for device {deviceType}: " +
                "variable is deprecated globally (IsEnabled=false).");

        // Cerca stato esistente
        var existingState = await _deviceStateRepository
            .GetByVariableAndDeviceAsync(variableId, deviceType, ct);

        if (existingState is not null)
        {
            existingState.IsEnabled = isEnabled;
            await _deviceStateRepository.UpdateAsync(existingState, ct);
        }
        else
        {
            var newState = new VariableDeviceStateEntity
            {
                VariableId = variableId,
                DeviceType = deviceType,
                IsEnabled = isEnabled
            };
            await _deviceStateRepository.AddAsync(newState, ct);
        }
    }

    public async Task<VariableDeviceState?> GetDeviceStateAsync(int variableId, DeviceType deviceType,
        CancellationToken ct = default)
    {
        var entity = await _deviceStateRepository
            .GetByVariableAndDeviceAsync(variableId, deviceType, ct);

        return entity is null ? null : VariableDeviceStateMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesAsync(int variableId,
        CancellationToken ct = default)
    {
        var entities = await _deviceStateRepository.GetByVariableIdAsync(variableId, ct);
        return VariableDeviceStateMapper.ToDomainList(entities);
    }

    public async Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesForDeviceAsync(
        DeviceType deviceType, CancellationToken ct = default)
    {
        var entities = await _deviceStateRepository.GetByDeviceTypeAsync(deviceType, ct);
        return entities.Select(VariableDeviceStateMapper.ToDomain).ToList();
    }
}
