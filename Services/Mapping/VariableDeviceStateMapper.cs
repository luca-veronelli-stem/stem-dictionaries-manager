using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

public static class VariableDeviceStateMapper
{
    public static VariableDeviceState ToDomain(VariableDeviceStateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return VariableDeviceState.Restore(
            entity.Id,
            entity.VariableId,
            entity.DeviceId,
            entity.IsEnabled);
    }

    public static VariableDeviceStateEntity ToEntity(VariableDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new VariableDeviceStateEntity
        {
            Id = domain.Id,
            VariableId = domain.VariableId,
            DeviceId = domain.DeviceId,
            IsEnabled = domain.IsEnabled
        };
    }

    public static void UpdateEntity(VariableDeviceStateEntity entity, VariableDeviceState domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.VariableId = domain.VariableId;
        entity.DeviceId = domain.DeviceId;
        entity.IsEnabled = domain.IsEnabled;
    }

    public static IReadOnlyList<VariableDeviceState> ToDomainList(
        IEnumerable<VariableDeviceStateEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
