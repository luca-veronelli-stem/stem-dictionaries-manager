using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per VariableDeviceState Entity ↔ Domain.
/// </summary>
public static class VariableDeviceStateMapper
{
    /// <summary>
    /// Converte VariableDeviceStateEntity in VariableDeviceState (Domain).
    /// </summary>
    public static VariableDeviceState ToDomain(VariableDeviceStateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return VariableDeviceState.Restore(
            entity.Id,
            entity.VariableId,
            entity.DeviceType,
            entity.IsEnabled);
    }

    /// <summary>
    /// Converte VariableDeviceState (Domain) in VariableDeviceStateEntity per creazione.
    /// </summary>
    public static VariableDeviceStateEntity ToEntity(VariableDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new VariableDeviceStateEntity
        {
            Id = domain.Id,
            VariableId = domain.VariableId,
            DeviceType = domain.DeviceType,
            IsEnabled = domain.IsEnabled
        };
    }

    /// <summary>
    /// Aggiorna VariableDeviceStateEntity esistente.
    /// </summary>
    public static void UpdateEntity(VariableDeviceStateEntity entity, VariableDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.VariableId = domain.VariableId;
        entity.DeviceType = domain.DeviceType;
        entity.IsEnabled = domain.IsEnabled;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<VariableDeviceState> ToDomainList(
        IEnumerable<VariableDeviceStateEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
