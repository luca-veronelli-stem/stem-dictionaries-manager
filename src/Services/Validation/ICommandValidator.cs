using Core.Models;

namespace Services.Validation;

/// <summary>
/// Validates <see cref="Command"/> business rules (name uniqueness and
/// code/IsResponse uniqueness) ahead of a create or update.
/// </summary>
public interface ICommandValidator
{
    Task<ValidationResult> ValidateForCreateAsync(Command command, CancellationToken ct = default);
    Task<ValidationResult> ValidateForUpdateAsync(Command command, CancellationToken ct = default);
}
