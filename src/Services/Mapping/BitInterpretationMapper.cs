using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for BitInterpretation Entity ↔ Domain.
/// </summary>
public static class BitInterpretationMapper
{
    /// <summary>
    /// Converts BitInterpretationEntity to BitInterpretation (Domain).
    /// </summary>
    public static BitInterpretation ToDomain(BitInterpretationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return BitInterpretation.Restore(
            entity.Id,
            entity.VariableId,
            entity.WordIndex,
            entity.BitIndex,
            entity.Meaning,
            entity.DictionaryId);
    }

    /// <summary>
    /// Converts BitInterpretation (Domain) to BitInterpretationEntity for creation.
    /// </summary>
    public static BitInterpretationEntity ToEntity(BitInterpretation domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new BitInterpretationEntity
        {
            Id = domain.Id,
            VariableId = domain.VariableId,
            DictionaryId = domain.DictionaryId,
            WordIndex = domain.WordIndex,
            BitIndex = domain.BitIndex,
            Meaning = domain.Meaning
        };
    }

    /// <summary>
    /// Updates an existing BitInterpretationEntity.
    /// </summary>
    public static void UpdateEntity(BitInterpretationEntity entity, BitInterpretation domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.VariableId = domain.VariableId;
        entity.DictionaryId = domain.DictionaryId;
        entity.WordIndex = domain.WordIndex;
        entity.BitIndex = domain.BitIndex;
        entity.Meaning = domain.Meaning;
    }

    /// <summary>
    /// Converts a list of entities to a list of domain models.
    /// </summary>
    public static IReadOnlyList<BitInterpretation> ToDomainList(IEnumerable<BitInterpretationEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
