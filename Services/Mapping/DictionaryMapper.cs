using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per Dictionary Entity ↔ Domain.
/// Domain v2: nessun DeviceType/BoardType. IsStandard flag.
/// </summary>
public static class DictionaryMapper
{
    /// <summary>
    /// Converte DictionaryEntity in Dictionary (Domain) senza variabili.
    /// </summary>
    public static Dictionary ToDomain(DictionaryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return Dictionary.Restore(entity.Id, entity.Name, entity.Description,
            entity.IsStandard, []);
    }

    /// <summary>
    /// Converte DictionaryEntity in Dictionary (Domain) con variabili.
    /// Richiede Variables caricate via Include.
    /// </summary>
    public static Dictionary ToDomainWithVariables(DictionaryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var variables = entity.Variables != null
            ? entity.Variables.Select(VariableMapper.ToDomain)
            : [];

        return Dictionary.Restore(entity.Id, entity.Name, entity.Description,
            entity.IsStandard, variables);
    }

    /// <summary>
    /// Converte Dictionary (Domain) in DictionaryEntity per creazione.
    /// </summary>
    public static DictionaryEntity ToEntity(Dictionary domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new DictionaryEntity
        {
            Id = domain.Id,
            Name = domain.Name,
            Description = domain.Description,
            IsStandard = domain.IsStandard
        };
    }

    /// <summary>
    /// Aggiorna DictionaryEntity esistente con dati da Dictionary (Domain).
    /// </summary>
    public static void UpdateEntity(DictionaryEntity entity, Dictionary domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Name = domain.Name;
        entity.Description = domain.Description;
        entity.IsStandard = domain.IsStandard;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models (senza variabili).
    /// </summary>
    public static IReadOnlyList<Dictionary> ToDomainList(IEnumerable<DictionaryEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
