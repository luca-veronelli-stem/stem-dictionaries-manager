using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per CommandDeviceState Entity ↔ Domain.
/// </summary>
public static class CommandDeviceStateMapper
{
    /// <summary>
    /// Converte CommandDeviceStateEntity in CommandDeviceState (Domain).
    /// </summary>
    public static CommandDeviceState ToDomain(CommandDeviceStateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return CommandDeviceState.Restore(
            entity.Id,
            entity.CommandId,
            entity.DeviceType,
            entity.IsEnabled);
    }

    /// <summary>
    /// Converte CommandDeviceState (Domain) in CommandDeviceStateEntity per creazione.
    /// </summary>
    public static CommandDeviceStateEntity ToEntity(CommandDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CommandDeviceStateEntity
        {
            Id = domain.Id,
            CommandId = domain.CommandId,
            DeviceType = domain.DeviceType,
            IsEnabled = domain.IsEnabled
        };
    }

    /// <summary>
    /// Aggiorna CommandDeviceStateEntity esistente.
    /// </summary>
    public static void UpdateEntity(CommandDeviceStateEntity entity, CommandDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.CommandId = domain.CommandId;
        entity.DeviceType = domain.DeviceType;
        entity.IsEnabled = domain.IsEnabled;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<CommandDeviceState> ToDomainList(IEnumerable<CommandDeviceStateEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
