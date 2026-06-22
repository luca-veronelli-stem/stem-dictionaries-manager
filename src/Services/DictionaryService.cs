using System.Text.Json;
using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Mapping;
using Services.Validation;

namespace Services;

/// <summary>
/// Dictionary service (aggregate root).
/// Domain v2: IsStandard flag, no BoardType/DeviceType.
/// </summary>
public class DictionaryService : IDictionaryService
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IVariableRepository _variableRepository;
    private readonly IAuditService _audit;
    private readonly ICurrentUserProvider _userProvider;
    private readonly IDictionaryValidator _dictionaryValidator;
    private readonly IVariableValidator _variableValidator;
    private readonly ILogger<DictionaryService> _logger;

    public DictionaryService(
        IDictionaryRepository dictionaryRepository,
        IVariableRepository variableRepository,
        IAuditService auditService,
        ICurrentUserProvider userProvider,
        ILogger<DictionaryService> logger)
    {
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(variableRepository);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(userProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _dictionaryRepository = dictionaryRepository;
        _variableRepository = variableRepository;
        _audit = auditService;
        _userProvider = userProvider;
        _logger = logger;
        _dictionaryValidator = new DictionaryValidator(dictionaryRepository);
        _variableValidator = new VariableValidator(variableRepository);
    }

    public async Task<Dictionary?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        DictionaryEntity? entity = await _dictionaryRepository.GetByIdAsync(id, ct);
        return entity is null ? null : DictionaryMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<DictionaryEntity> entities = await _dictionaryRepository.GetAllWithVariablesAsync(ct);
        return [.. entities.Select(DictionaryMapper.ToDomainWithVariables)];
    }

    public async Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        (await _dictionaryValidator.ValidateForCreateAsync(dictionary, ct)).EnsureValid();

        DictionaryEntity entity = DictionaryMapper.ToEntity(dictionary);
        DictionaryEntity created = await _dictionaryRepository.AddAsync(entity, ct);
        Dictionary result = DictionaryMapper.ToDomain(created);

        _logger.LogInformation("Created dictionary {DictionaryId} ({Name})", result.Id, result.Name);

        await _audit.LogCreateAsync(AuditEntityType.Dictionary, result.Id,
            _userProvider.CurrentUserId ?? 0,
            JsonSerializer.Serialize(result), ct: ct);

        return result;
    }

    public async Task UpdateAsync(Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        DictionaryEntity entity = await _dictionaryRepository.GetByIdAsync(dictionary.Id, ct)
            ?? throw new KeyNotFoundException(
                $"Dictionary '{dictionary.Name}' (Id={dictionary.Id}) not found.");

        (await _dictionaryValidator.ValidateForUpdateAsync(dictionary, ct)).EnsureValid();

        Dictionary previous = DictionaryMapper.ToDomain(entity);
        string prevJson = JsonSerializer.Serialize(previous);

        DictionaryMapper.UpdateEntity(entity, dictionary);
        await _dictionaryRepository.UpdateAsync(entity, ct);

        await _audit.LogUpdateAsync(AuditEntityType.Dictionary, dictionary.Id,
            _userProvider.CurrentUserId ?? 0,
            prevJson, JsonSerializer.Serialize(dictionary), ct: ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        DictionaryEntity? entity = await _dictionaryRepository.GetByIdAsync(id, ct);
        if (entity is not null)
        {
            Dictionary previous = DictionaryMapper.ToDomain(entity);
            await _dictionaryRepository.DeleteAsync(id, ct);
            await _audit.LogDeleteAsync(AuditEntityType.Dictionary, id,
                _userProvider.CurrentUserId ?? 0,
                JsonSerializer.Serialize(previous), ct: ct);
        }
        else
        {
            await _dictionaryRepository.DeleteAsync(id, ct);
        }
    }

    public async Task<Dictionary?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        DictionaryEntity? entity = await _dictionaryRepository.GetByNameAsync(name, ct);
        return entity is null ? null : DictionaryMapper.ToDomain(entity);
    }

    public async Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default)
    {
        DictionaryEntity? entity = await _dictionaryRepository.GetStandardDictionaryAsync(ct);
        return entity is null ? null : DictionaryMapper.ToDomainWithVariables(entity);
    }

    public async Task<Dictionary?> GetWithVariablesAsync(int id, CancellationToken ct = default)
    {
        DictionaryEntity? entity = await _dictionaryRepository.GetWithVariablesAsync(id, ct);
        return entity is null ? null : DictionaryMapper.ToDomainWithVariables(entity);
    }

    public async Task<Variable> AddVariableAsync(int dictionaryId, Variable variable,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (!await _dictionaryRepository.ExistsAsync(dictionaryId, ct))
        {
            throw new KeyNotFoundException($"Dictionary (Id={dictionaryId}) not found.");
        }

        (await _variableValidator.ValidateForCreateAsync(dictionaryId, variable, ct)).EnsureValid();

        VariableEntity entity = VariableMapper.ToEntity(variable, dictionaryId);
        VariableEntity created = await _variableRepository.AddAsync(entity, ct);
        Variable result = VariableMapper.ToDomain(created);

        await _audit.LogCreateAsync(AuditEntityType.Variable, result.Id,
            _userProvider.CurrentUserId ?? 0,
            JsonSerializer.Serialize(result), ct: ct);

        return result;
    }

    public async Task RemoveVariableAsync(int dictionaryId, int variableId,
        CancellationToken ct = default)
    {
        if (!await _dictionaryRepository.ExistsAsync(dictionaryId, ct))
        {
            throw new KeyNotFoundException(
                $"Dictionary (Id={dictionaryId}) not found.");
        }

        VariableEntity variable = await _variableRepository.GetByIdAsync(variableId, ct)
            ?? throw new KeyNotFoundException(
                $"Variable (Id={variableId}) not found.");

        if (variable.DictionaryId != dictionaryId)
        {
            throw new InvalidOperationException(
                $"Variable '{variable.Name}' does not belong to this dictionary.");
        }

        Variable previous = VariableMapper.ToDomain(variable);
        await _variableRepository.DeleteAsync(variableId, ct);

        await _audit.LogDeleteAsync(AuditEntityType.Variable, variableId,
            _userProvider.CurrentUserId ?? 0,
            JsonSerializer.Serialize(previous), ct: ct);
    }
}
