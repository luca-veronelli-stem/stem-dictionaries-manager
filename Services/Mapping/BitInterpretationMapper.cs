using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per BitInterpretation Entity ↔ Domain.
/// </summary>
public static class BitInterpretationMapper
{
    /// <summary>
    /// Converte BitInterpretationEntity in BitInterpretation (Domain).
    /// </summary>
    public static BitInterpretation ToDomain(BitInterpretationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return BitInterpretation.Restore(
            entity.Id,
            entity.VariableId,
            entity.WordIndex,
            entity.BitIndex,
            entity.Meaning);
    }

    /// <summary>
    /// Converte BitInterpretation (Domain) in BitInterpretationEntity per creazione.
    /// </summary>
    public static BitInterpretationEntity ToEntity(BitInterpretation domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new BitInterpretationEntity
        {
            Id = domain.Id,
            VariableId = domain.VariableId,
            WordIndex = domain.WordIndex,
            BitIndex = domain.BitIndex,
            Meaning = domain.Meaning
        };
    }

    /// <summary>
    /// Aggiorna BitInterpretationEntity esistente.
    /// </summary>
    public static void UpdateEntity(BitInterpretationEntity entity, BitInterpretation domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.VariableId = domain.VariableId;
        entity.WordIndex = domain.WordIndex;
        entity.BitIndex = domain.BitIndex;
        entity.Meaning = domain.Meaning;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<BitInterpretation> ToDomainList(IEnumerable<BitInterpretationEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
