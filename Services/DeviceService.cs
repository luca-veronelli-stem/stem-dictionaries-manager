using Core.Models;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione dispositivi.
/// BR-014: MachineCode unico e > 0.
/// BR-015: MachineCode 6 riservato per BLE Module.
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _repository;

    public DeviceService(IDeviceRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<Device?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : DeviceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetAllAsync(ct);
        return DeviceMapper.ToDomainList(entities);
    }

    public async Task<Device> AddAsync(Device device, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        // Unicità nome
        var existing = await _repository.GetByNameAsync(device.Name, ct);
        if (existing is not null)
            throw new InvalidOperationException(
                $"Un dispositivo con nome '{device.Name}' esiste già.");

        // Unicità MachineCode
        var byCode = await _repository.GetByMachineCodeAsync(device.MachineCode, ct);
        if (byCode is not null)
            throw new InvalidOperationException(
                $"Un dispositivo con MachineCode {device.MachineCode} esiste già.");

        var entity = DeviceMapper.ToEntity(device);
        var created = await _repository.AddAsync(entity, ct);
        return DeviceMapper.ToDomain(created);
    }

    public async Task UpdateAsync(Device device, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var entity = await _repository.GetByIdAsync(device.Id, ct)
            ?? throw new KeyNotFoundException(
                $"Device '{device.Name}' (Id={device.Id}) not found.");

        // Unicità nome (esclude se stesso)
        var byName = await _repository.GetByNameAsync(device.Name, ct);
        if (byName is not null && byName.Id != device.Id)
            throw new InvalidOperationException(
                $"Un dispositivo con nome '{device.Name}' esiste già.");

        // Unicità MachineCode (esclude se stesso)
        var byCode = await _repository.GetByMachineCodeAsync(device.MachineCode, ct);
        if (byCode is not null && byCode.Id != device.Id)
            throw new InvalidOperationException(
                $"Un dispositivo con MachineCode {device.MachineCode} esiste già.");

        DeviceMapper.UpdateEntity(entity, device);
        await _repository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
    }

    public async Task<Device?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var entity = await _repository.GetByNameAsync(name, ct);
        return entity is null ? null : DeviceMapper.ToDomain(entity);
    }
}
