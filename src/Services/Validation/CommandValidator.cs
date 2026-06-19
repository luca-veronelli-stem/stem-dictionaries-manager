using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;

namespace Services.Validation;

/// <inheritdoc cref="ICommandValidator" />
public sealed class CommandValidator : ICommandValidator
{
    private readonly ICommandRepository _commandRepository;

    public CommandValidator(ICommandRepository commandRepository)
    {
        ArgumentNullException.ThrowIfNull(commandRepository);
        _commandRepository = commandRepository;
    }

    public async Task<ValidationResult> ValidateForCreateAsync(Command command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        CommandEntity? existingByName = await _commandRepository.GetByNameAsync(command.Name, ct);
        if (existingByName is not null)
        {
            errors.Add($"Command with name '{command.Name}' already exists.");
        }

        CommandEntity? existingByCode = await _commandRepository.GetByCodeAsync(
            command.CodeHigh, command.CodeLow, command.IsResponse, ct);
        if (existingByCode is not null)
        {
            errors.Add(
                $"Command with code 0x{command.CodeHigh:X2}{command.CodeLow:X2} " +
                $"(IsResponse={command.IsResponse}) already exists ('{existingByCode.Name}').");
        }

        return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(false, errors);
    }

    public async Task<ValidationResult> ValidateForUpdateAsync(Command command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        CommandEntity? current = await _commandRepository.GetByIdAsync(command.Id, ct);
        if (current is null)
        {
            // The service owns the not-found contract (KeyNotFoundException).
            return ValidationResult.Success;
        }

        if (current.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        CommandEntity? existingByName = await _commandRepository.GetByNameAsync(command.Name, ct);
        return existingByName is null
            ? ValidationResult.Success
            : ValidationResult.Failure($"Command with name '{command.Name}' already exists.");
    }
}
