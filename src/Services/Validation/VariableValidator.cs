using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;

namespace Services.Validation;

/// <inheritdoc cref="IVariableValidator" />
public sealed class VariableValidator : IVariableValidator
{
    private readonly IVariableRepository _variableRepository;

    public VariableValidator(IVariableRepository variableRepository)
    {
        ArgumentNullException.ThrowIfNull(variableRepository);
        _variableRepository = variableRepository;
    }

    public async Task<ValidationResult> ValidateForCreateAsync(int dictionaryId, Variable variable,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        VariableEntity? existing = await _variableRepository.GetByAddressAsync(
            dictionaryId, variable.AddressHigh, variable.AddressLow, ct);

        return existing is null
            ? ValidationResult.Success
            : ValidationResult.Failure(AddressConflictMessage(variable));
    }

    public async Task<ValidationResult> ValidateForUpdateAsync(Variable variable,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(variable);

        VariableEntity? current = await _variableRepository.GetByIdAsync(variable.Id, ct);
        if (current is null)
        {
            // The service owns the not-found contract (KeyNotFoundException).
            return ValidationResult.Success;
        }

        bool addressChanged =
            current.AddressHigh != variable.AddressHigh || current.AddressLow != variable.AddressLow;
        if (!addressChanged)
        {
            return ValidationResult.Success;
        }

        VariableEntity? existing = await _variableRepository.GetByAddressAsync(
            current.DictionaryId, variable.AddressHigh, variable.AddressLow, ct);

        return existing is not null && existing.Id != variable.Id
            ? ValidationResult.Failure(AddressConflictMessage(variable))
            : ValidationResult.Success;
    }

    private static string AddressConflictMessage(Variable variable) =>
        $"Variable with address 0x{variable.AddressHigh:X2}{variable.AddressLow:X2} " +
        "already exists in this dictionary.";
}
