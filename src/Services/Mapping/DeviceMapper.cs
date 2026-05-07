using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per Device Entity ↔ Domain.
/// </summary>
public static class DeviceMapper
{
    /// <summary>
    /// Converte DeviceEntity in Device (Domain).
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
    /// Converte Device (Domain) in DeviceEntity per creazione.
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
    /// Aggiorna DeviceEntity esistente con dati da Device (Domain).
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
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<Device> ToDomainList(IEnumerable<DeviceEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
