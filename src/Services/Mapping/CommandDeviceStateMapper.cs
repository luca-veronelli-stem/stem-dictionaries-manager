using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

public static class CommandDeviceStateMapper
{
    public static CommandDeviceState ToDomain(CommandDeviceStateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return CommandDeviceState.Restore(
            entity.Id,
            entity.CommandId,
            entity.DeviceId,
            entity.IsEnabled);
    }

    public static CommandDeviceStateEntity ToEntity(CommandDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CommandDeviceStateEntity
        {
            Id = domain.Id,
            CommandId = domain.CommandId,
            DeviceId = domain.DeviceId,
            IsEnabled = domain.IsEnabled
        };
    }

    public static void UpdateEntity(CommandDeviceStateEntity entity, CommandDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.CommandId = domain.CommandId;
        entity.DeviceId = domain.DeviceId;
        entity.IsEnabled = domain.IsEnabled;
    }

    public static IReadOnlyList<CommandDeviceState> ToDomainList(IEnumerable<CommandDeviceStateEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
