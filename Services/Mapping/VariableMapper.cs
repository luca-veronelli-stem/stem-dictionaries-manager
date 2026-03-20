using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per Variable Entity ↔ Domain.
/// Gestisce la complessità di DataTypeKind + DataTypeParam + DataTypeRaw.
/// </summary>
public static class VariableMapper
{
    /// <summary>
    /// Converte VariableEntity in Variable (Domain).
    /// </summary>
    public static Variable ToDomain(VariableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Variable.Restore(
            id: entity.Id,
            name: entity.Name,
            addressHigh: entity.AddressHigh,
            addressLow: entity.AddressLow,
            dataTypeKind: entity.DataTypeKind,
            dataTypeRaw: entity.DataTypeRaw,
            dataTypeParam: entity.DataTypeParam,
            accessMode: entity.AccessMode,
            isEnabled: entity.IsEnabled,
            format: null, // Format non è presente in Entity
            minValue: entity.MinValue,
            maxValue: entity.MaxValue,
            unit: entity.Unit,
            usage: entity.Usage,
            description: entity.Description);
    }

    /// <summary>
    /// Converte Variable (Domain) in VariableEntity per creazione.
    /// </summary>
    public static VariableEntity ToEntity(Variable domain, int dictionaryId)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new VariableEntity
        {
            Id = domain.Id,
            DictionaryId = dictionaryId,
            Name = domain.Name,
            AddressHigh = domain.AddressHigh,
            AddressLow = domain.AddressLow,
            DataTypeKind = domain.DataTypeKind,
            DataTypeParam = domain.DataTypeParam,
            DataTypeRaw = domain.DataTypeRaw,
            AccessMode = domain.AccessMode,
            IsEnabled = domain.IsEnabled,
            MinValue = domain.MinValue,
            MaxValue = domain.MaxValue,
            Unit = domain.Unit,
            Usage = domain.Usage,
            Description = domain.Description
        };
    }

    /// <summary>
    /// Aggiorna VariableEntity esistente con dati da Variable (Domain).
    /// Preserva Id, DictionaryId e audit fields.
    /// </summary>
    public static void UpdateEntity(VariableEntity entity, Variable domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Name = domain.Name;
        entity.AddressHigh = domain.AddressHigh;
        entity.AddressLow = domain.AddressLow;
        entity.DataTypeKind = domain.DataTypeKind;
        entity.DataTypeParam = domain.DataTypeParam;
        entity.DataTypeRaw = domain.DataTypeRaw;
        entity.AccessMode = domain.AccessMode;
        entity.IsEnabled = domain.IsEnabled;
        entity.MinValue = domain.MinValue;
        entity.MaxValue = domain.MaxValue;
        entity.Unit = domain.Unit;
        entity.Usage = domain.Usage;
        entity.Description = domain.Description;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<Variable> ToDomainList(IEnumerable<VariableEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
