using Core.Models;

namespace Services.Validation;

/// <summary>
/// Validates <see cref="Variable"/> business rules (address uniqueness within a
/// dictionary) ahead of a create or update.
/// </summary>
public interface IVariableValidator
{
    Task<ValidationResult> ValidateForCreateAsync(int dictionaryId, Variable variable,
        CancellationToken ct = default);
    Task<ValidationResult> ValidateForUpdateAsync(Variable variable, CancellationToken ct = default);
}
