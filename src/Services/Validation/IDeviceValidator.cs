using Core.Models;

namespace Services.Validation;

/// <summary>
/// Validates <see cref="Device"/> business rules (name uniqueness and
/// MachineCode uniqueness) ahead of a create or update.
/// </summary>
public interface IDeviceValidator
{
    Task<ValidationResult> ValidateForCreateAsync(Device device, CancellationToken ct = default);
    Task<ValidationResult> ValidateForUpdateAsync(Device device, CancellationToken ct = default);
}
