using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for Device Entity ↔ Domain.
/// </summary>
public static class DeviceMapper
{
    /// <summary>
    /// Converts DeviceEntity to Device (Domain).
    /// </summary>
    public static Device ToDomain(DeviceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Device.Restore(
            entity.Id,
            entity.Name,
            entity.MachineCode,
            entity.Description);
    }

    /// <summary>
    /// Converts Device (Domain) to DeviceEntity for creation.
    /// </summary>
    public static DeviceEntity ToEntity(Device domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new DeviceEntity
        {
            Id = domain.Id,
            Name = domain.Name,
            MachineCode = domain.MachineCode,
            Description = domain.Description
        };
    }

    /// <summary>
    /// Updates an existing DeviceEntity with data from Device (Domain).
    /// </summary>
    public static void UpdateEntity(DeviceEntity entity, Device domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Name = domain.Name;
        entity.MachineCode = domain.MachineCode;
        entity.Description = domain.Description;
    }

    /// <summary>
    /// Converts a list of entities to a list of domain models.
    /// </summary>
    public static IReadOnlyList<Device> ToDomainList(IEnumerable<DeviceEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
