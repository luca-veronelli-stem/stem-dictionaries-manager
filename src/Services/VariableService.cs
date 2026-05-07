using System.Text.Json;
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
/// v7: DeviceState → StandardVariableOverride (per-dizionario).
/// </summary>
public class VariableService : IVariableService
{
    private readonly IVariableRepository _repository;
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IBitInterpretationRepository _bitInterpretationRepository;
    private readonly IStandardVariableOverrideRepository _overrideRepository;
    private readonly IAuditService _audit;
    private readonly ICurrentUserProvider _userProvider;

    public VariableService(
        IVariableRepository repository,
        IDictionaryRepository dictionaryRepository,
        IBitInterpretationRepository bitInterpretationRepository,
        IStandardVariableOverrideRepository overrideRepository,
        IAuditService auditService,
        ICurrentUserProvider userProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(bitInterpretationRepository);
        ArgumentNullException.ThrowIfNull(overrideRepository);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(userProvider);
        _repository = repository;
        _dictionaryRepository = dictionaryRepository;
        _bitInterpretationRepository = bitInterpretationRepository;
        _overrideRepository = overrideRepository;
        _audit = auditService;
        _userProvider = userProvider;
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
            throw new KeyNotFoundException(
                $"Dictionary (Id={dictionaryId}) not found.");

        // Verifica unicità indirizzo nel dizionario
        var existingByAddress = await _repository.GetByAddressAsync(
            dictionaryId, variable.AddressHigh, variable.AddressLow, ct);
        if (existingByAddress is not null)
            throw new InvalidOperationException(
                $"Variable with address 0x{variable.AddressHigh:X2}{variable.AddressLow:X2} " +
                $"already exists in this dictionary.");

        var entity = VariableMapper.ToEntity(variable, dictionaryId);
        var created = await _repository.AddAsync(entity, ct);
        var result = VariableMapper.ToDomain(created);

        await _audit.LogCreateAsync(AuditEntityType.Variable, result.Id,
            _userProvider.CurrentUserId ?? 0,
            JsonSerializer.Serialize(result), ct: ct);

        return result;
    }

    public async Task UpdateAsync(Variable variable, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        var entity = await _repository.GetByIdAsync(variable.Id, ct)
            ?? throw new KeyNotFoundException(
                $"Variable '{variable.Name}' (Id={variable.Id}) not found.");

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

        var previous = VariableMapper.ToDomain(entity);
        var prevJson = JsonSerializer.Serialize(previous);

        VariableMapper.UpdateEntity(entity, variable);
        await _repository.UpdateAsync(entity, ct);

        await _audit.LogUpdateAsync(AuditEntityType.Variable, variable.Id,
            _userProvider.CurrentUserId ?? 0,
            prevJson, JsonSerializer.Serialize(variable), ct: ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is not null)
        {
            var previous = VariableMapper.ToDomain(entity);
            await _repository.DeleteAsync(id, ct);
            await _audit.LogDeleteAsync(AuditEntityType.Variable, id,
                _userProvider.CurrentUserId ?? 0,
                JsonSerializer.Serialize(previous), ct: ct);
        }
        else
        {
            await _repository.DeleteAsync(id, ct);
        }
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
            throw new KeyNotFoundException(
                $"Variable (Id={variableId}) not found.");

        var entities = await _bitInterpretationRepository.GetByVariableIdAsync(variableId, ct);

        // Ritorna solo le interpretazioni template (DictionaryId = null)
        var template = BitInterpretationMapper.ToDomainList(entities)
            .Where(b => b.DictionaryId is null)
            .ToList();

        return template;
    }

    public async Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsForDictionaryAsync(
        int variableId, int dictionaryId, CancellationToken ct = default)
    {
        var variableExists = await _repository.ExistsAsync(variableId, ct);
        if (!variableExists)
            throw new KeyNotFoundException(
                $"Variable (Id={variableId}) not found.");

        var entities = await _bitInterpretationRepository
            .GetByVariableAndDictionaryAsync(variableId, dictionaryId, ct);

        var allBits = BitInterpretationMapper.ToDomainList(entities);

        // BR-018: per ogni (WordIndex, BitIndex), per-dizionario ha priorità su template
        var merged = allBits
            .GroupBy(b => (b.WordIndex, b.BitIndex))
            .Select(g => g.FirstOrDefault(b => b.DictionaryId is not null) ?? g.First())
            .OrderBy(b => b.WordIndex)
            .ThenBy(b => b.BitIndex)
            .ToList();

        return merged;
    }

    public async Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(interpretation);

        // Verifica che la variabile esista ed sia bitmapped
        var variable = await _repository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException(
                $"Variable (Id={variableId}) not found.");

        if (variable.DataTypeKind != DataTypeKind.Bitmapped)
            throw new InvalidOperationException(
                $"Variable '{variable.Name}' is not bitmapped. Cannot add bit interpretation.");

        var entity = BitInterpretationMapper.ToEntity(interpretation);
        entity.VariableId = variableId;

        var created = await _bitInterpretationRepository.AddAsync(entity, ct);

        return BitInterpretationMapper.ToDomain(created);
    }

    public async Task UpdateBitInterpretationsAsync(int variableId,
        IEnumerable<BitInterpretation> interpretations, CancellationToken ct = default)
    {
        await UpdateBitInterpretationsForDictionaryAsync(variableId, null, interpretations, ct);
    }

    public async Task UpdateBitInterpretationsForDictionaryAsync(int variableId,
        int? dictionaryId, IEnumerable<BitInterpretation> interpretations,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(interpretations);

        var variable = await _repository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException(
                $"Variable (Id={variableId}) not found.");

        if (variable.DataTypeKind != DataTypeKind.Bitmapped)
            throw new InvalidOperationException(
                $"Variable '{variable.Name}' is not bitmapped. Cannot update bit interpretations.");

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
        await _bitInterpretationRepository.SyncByVariableIdAsync(variableId, dictionaryId, entities, ct);
    }

    // === StandardVariableOverride Management ===

    public async Task SetOverrideAsync(int dictionaryId, int standardVariableId, bool isEnabled,
        string? description = null, CancellationToken ct = default)
    {
        // Verifica che la variabile standard esista
        var variable = await _repository.GetByIdAsync(standardVariableId, ct)
            ?? throw new KeyNotFoundException(
                $"Variable (Id={standardVariableId}) not found.");

        // BR-011: override isEnabled=true vietato se Variable.IsEnabled=false
        if (!variable.IsEnabled && isEnabled)
            throw new InvalidOperationException(
                $"Cannot enable variable '{variable.Name}' for dictionary {dictionaryId}: " +
                "variable is deprecated globally (IsEnabled=false).");

        // Cerca override esistente
        var existing = await _overrideRepository
            .GetByDictionaryAndVariableAsync(dictionaryId, standardVariableId, ct);

        if (existing is not null)
        {
            var prevOverride = StandardVariableOverrideMapper.ToDomain(existing);
            var prevJson = JsonSerializer.Serialize(prevOverride);

            existing.IsEnabled = isEnabled;
            existing.Description = description;
            await _overrideRepository.UpdateAsync(existing, ct);

            var updated = StandardVariableOverrideMapper.ToDomain(existing);
            await _audit.LogUpdateAsync(
                AuditEntityType.StandardVariableOverride, existing.Id,
                _userProvider.CurrentUserId ?? 0,
                prevJson, JsonSerializer.Serialize(updated), ct: ct);
        }
        else
        {
            var newOverride = new StandardVariableOverrideEntity
            {
                DictionaryId = dictionaryId,
                StandardVariableId = standardVariableId,
                IsEnabled = isEnabled,
                Description = description
            };
            var created = await _overrideRepository.AddAsync(newOverride, ct);

            var domain = StandardVariableOverrideMapper.ToDomain(created);
            await _audit.LogCreateAsync(
                AuditEntityType.StandardVariableOverride, created.Id,
                _userProvider.CurrentUserId ?? 0,
                JsonSerializer.Serialize(domain), ct: ct);
        }
    }

    public async Task<StandardVariableOverride?> GetOverrideAsync(int dictionaryId,
        int standardVariableId, CancellationToken ct = default)
    {
        var entity = await _overrideRepository
            .GetByDictionaryAndVariableAsync(dictionaryId, standardVariableId, ct);

        return entity is null ? null : StandardVariableOverrideMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<StandardVariableOverride>> GetOverridesByDictionaryAsync(
        int dictionaryId, CancellationToken ct = default)
    {
        var entities = await _overrideRepository.GetByDictionaryIdAsync(dictionaryId, ct);
        return StandardVariableOverrideMapper.ToDomainList(entities);
    }

    public async Task<IReadOnlyList<StandardVariableOverride>> GetOverridesByVariableAsync(
        int standardVariableId, CancellationToken ct = default)
    {
        var entities = await _overrideRepository.GetByVariableIdAsync(standardVariableId, ct);
        return StandardVariableOverrideMapper.ToDomainList(entities);
    }
}
