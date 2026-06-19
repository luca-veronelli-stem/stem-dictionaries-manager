using Infrastructure.Entities;
using Infrastructure.Interfaces;

namespace Services.Validation;

/// <inheritdoc cref="IDictionaryValidator" />
public sealed class DictionaryValidator : IDictionaryValidator
{
    private readonly IDictionaryRepository _dictionaryRepository;

    public DictionaryValidator(IDictionaryRepository dictionaryRepository)
    {
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        _dictionaryRepository = dictionaryRepository;
    }

    public async Task<ValidationResult> ValidateForCreateAsync(
        Core.Models.Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        var errors = new List<string>();
        await AddNameConflictAsync(dictionary.Name, errors, ct);
        if (dictionary.IsStandard)
        {
            await AddStandardConflictAsync(errors, ct);
        }

        return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(false, errors);
    }

    public async Task<ValidationResult> ValidateForUpdateAsync(
        Core.Models.Dictionary dictionary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        DictionaryEntity? current = await _dictionaryRepository.GetByIdAsync(dictionary.Id, ct);
        if (current is null)
        {
            // The service owns the not-found contract (KeyNotFoundException).
            return ValidationResult.Success;
        }

        var errors = new List<string>();
        if (!current.Name.Equals(dictionary.Name, StringComparison.OrdinalIgnoreCase))
        {
            await AddNameConflictAsync(dictionary.Name, errors, ct);
        }

        if (dictionary.IsStandard && !current.IsStandard)
        {
            await AddStandardConflictAsync(errors, ct);
        }

        return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(false, errors);
    }

    private async Task AddNameConflictAsync(string name, List<string> errors, CancellationToken ct)
    {
        DictionaryEntity? existing = await _dictionaryRepository.GetByNameAsync(name, ct);
        if (existing is not null)
        {
            errors.Add($"Dictionary with name '{name}' already exists.");
        }
    }

    private async Task AddStandardConflictAsync(List<string> errors, CancellationToken ct)
    {
        DictionaryEntity? existingStandard = await _dictionaryRepository.GetStandardDictionaryAsync(ct);
        if (existingStandard is not null)
        {
            errors.Add("A Standard dictionary already exists. Only one is allowed (BR-004).");
        }
    }
}
