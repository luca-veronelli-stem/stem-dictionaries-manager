using Core.Models;

namespace Services.Validation;

/// <summary>
/// Validates <see cref="Dictionary"/> business rules (name uniqueness and the
/// single-Standard-dictionary rule BR-004) ahead of a create or update.
/// </summary>
public interface IDictionaryValidator
{
    Task<ValidationResult> ValidateForCreateAsync(Dictionary dictionary, CancellationToken ct = default);
    Task<ValidationResult> ValidateForUpdateAsync(Dictionary dictionary, CancellationToken ct = default);
}
