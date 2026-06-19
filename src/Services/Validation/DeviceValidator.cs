using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;

namespace Services.Validation;

/// <inheritdoc cref="IDeviceValidator" />
public sealed class DeviceValidator : IDeviceValidator
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceValidator(IDeviceRepository deviceRepository)
    {
        ArgumentNullException.ThrowIfNull(deviceRepository);
        _deviceRepository = deviceRepository;
    }

    public Task<ValidationResult> ValidateForCreateAsync(Device device,
        CancellationToken ct = default) => ValidateAsync(device, excludeId: null, ct);

    public Task<ValidationResult> ValidateForUpdateAsync(Device device,
        CancellationToken ct = default) => ValidateAsync(device, excludeId: device?.Id, ct);

    private async Task<ValidationResult> ValidateAsync(Device device, int? excludeId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(device);

        var errors = new List<string>();

        DeviceEntity? byName = await _deviceRepository.GetByNameAsync(device.Name, ct);
        if (byName is not null && byName.Id != excludeId)
        {
            errors.Add($"A device with name '{device.Name}' already exists.");
        }

        DeviceEntity? byCode = await _deviceRepository.GetByMachineCodeAsync(device.MachineCode, ct);
        if (byCode is not null && byCode.Id != excludeId)
        {
            errors.Add($"A device with MachineCode {device.MachineCode} already exists.");
        }

        return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(false, errors);
    }
}
