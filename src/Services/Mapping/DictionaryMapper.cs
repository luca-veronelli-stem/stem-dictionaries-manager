using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for Dictionary Entity ↔ Domain.
/// Domain v2: no DeviceType/BoardType. IsStandard flag.
/// </summary>
public static class DictionaryMapper
{
    /// <summary>
    /// Converts DictionaryEntity to Dictionary (Domain) without variables.
    /// </summary>
    public static Dictionary ToDomain(DictionaryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return Dictionary.Restore(entity.Id, entity.Name, entity.Description,
            entity.IsStandard, []);
    }

    /// <summary>
    /// Converts DictionaryEntity to Dictionary (Domain) with variables.
    /// Requires Variables loaded via Include.
    /// </summary>
    public static Dictionary ToDomainWithVariables(DictionaryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        IEnumerable<Variable> variables = entity.Variables != null
            ? entity.Variables.Select(VariableMapper.ToDomain)
            : [];

        return Dictionary.Restore(entity.Id, entity.Name, entity.Description,
            entity.IsStandard, variables);
    }

    /// <summary>
    /// Converts Dictionary (Domain) to DictionaryEntity for creation.
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
    /// Updates an existing DictionaryEntity with data from Dictionary (Domain).
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
    /// Converts a list of entities to a list of domain models (without variables).
    /// </summary>
    public static IReadOnlyList<Dictionary> ToDomainList(IEnumerable<DictionaryEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
