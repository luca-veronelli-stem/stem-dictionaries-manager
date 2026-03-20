using Core.Enums;
using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per Dictionary Entity ↔ Domain.
/// Dictionary è l'aggregate root: include mapping delle Variables.
/// </summary>
public static class DictionaryMapper
{
    /// <summary>
    /// Converte DictionaryEntity in Dictionary (Domain).
    /// Senza variabili (lazy loading).
    /// </summary>
    public static Dictionary ToDomain(DictionaryEntity entity, BoardType? boardType = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Se boardType non fornito, prova a usare navigation property
        if (boardType == null && entity.BoardType != null)
        {
            boardType = BoardTypeMapper.ToDomain(entity.BoardType);
        }

        return Dictionary.Restore(entity.Id, entity.Name, entity.DeviceType, boardType, entity.Description, []);
    }

    /// <summary>
    /// Converte DictionaryEntity in Dictionary (Domain) con variabili.
    /// Richiede Variables caricate via Include.
    /// </summary>
    public static Dictionary ToDomainWithVariables(DictionaryEntity entity, BoardType? boardType = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Se boardType non fornito, prova a usare navigation property
        if (boardType == null && entity.BoardType != null)
        {
            boardType = BoardTypeMapper.ToDomain(entity.BoardType);
        }

        var variables = entity.Variables != null
            ? entity.Variables.Select(VariableMapper.ToDomain)
            : [];

        return Dictionary.Restore(entity.Id, entity.Name, entity.DeviceType, boardType, entity.Description, variables);
    }

    /// <summary>
    /// Converte Dictionary (Domain) in DictionaryEntity per creazione.
    /// Non include variabili (gestite separatamente).
    /// </summary>
    public static DictionaryEntity ToEntity(Dictionary domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new DictionaryEntity
        {
            Id = domain.Id,
            Name = domain.Name,
            Description = domain.Description,
            DeviceType = domain.DeviceType,
            BoardTypeId = domain.BoardType?.Id
        };
    }

    /// <summary>
    /// Aggiorna DictionaryEntity esistente con dati da Dictionary (Domain).
    /// Non aggiorna variabili (gestite separatamente).
    /// </summary>
    public static void UpdateEntity(DictionaryEntity entity, Dictionary domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Name = domain.Name;
        entity.Description = domain.Description;
        entity.DeviceType = domain.DeviceType;
        entity.BoardTypeId = domain.BoardType?.Id;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models (senza variabili).
    /// </summary>
    public static IReadOnlyList<Dictionary> ToDomainList(IEnumerable<DictionaryEntity> entities)
    {
        return [.. entities.Select(e => ToDomain(e))];
    }
}
