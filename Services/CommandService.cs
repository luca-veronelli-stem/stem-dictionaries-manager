using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione comandi.
/// </summary>
public class CommandService : ICommandService
{
    private readonly ICommandRepository _repository;
    private readonly ICommandDeviceStateRepository _deviceStateRepository;

    public CommandService(ICommandRepository repository, ICommandDeviceStateRepository deviceStateRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(deviceStateRepository);
        _repository = repository;
        _deviceStateRepository = deviceStateRepository;
    }

    // === CRUD Base ===

    public async Task<Command?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : CommandMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetAllAsync(ct);
        return CommandMapper.ToDomainList(entities);
    }

    public async Task<Command> AddAsync(Command command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Verifica unicità codice
        var existing = await _repository.GetByCodeAsync(
            command.CodeHigh, command.CodeLow, command.IsResponse, ct);
        if (existing is not null)
            throw new InvalidOperationException(
                $"Command with code 0x{command.CodeHigh:X2}{command.CodeLow:X2} " +
                $"(IsResponse={command.IsResponse}) already exists.");

        var entity = CommandMapper.ToEntity(command);
        var created = await _repository.AddAsync(entity, ct);
        return CommandMapper.ToDomain(created);
    }

    public async Task UpdateAsync(Command command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Command with Id {command.Id} not found.");

        CommandMapper.UpdateEntity(entity, command);
        await _repository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
    }

    // === Query Specifiche ===

    public async Task<Command?> GetByCodeAsync(byte codeHigh, byte codeLow, bool isResponse,
        CancellationToken ct = default)
    {
        var entity = await _repository.GetByCodeAsync(codeHigh, codeLow, isResponse, ct);
        return entity is null ? null : CommandMapper.ToDomain(entity);
    }

    // === DeviceState Management ===

    public async Task<Command?> GetWithDeviceStatesAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetWithDeviceStatesAsync(id, ct);
        if (entity is null)
            return null;

        var command = CommandMapper.ToDomain(entity);
        // Nota: DeviceStates sono caricati ma non esposti nel Domain Model Command.
        // Per accedere agli stati, usare GetDeviceStateAsync o SetDeviceStateAsync.
        return command;
    }

    public async Task SetDeviceStateAsync(int commandId, DeviceType deviceType, bool isEnabled,
        CancellationToken ct = default)
    {
        // Verifica che il comando esista
        _ = await _repository.GetByIdAsync(commandId, ct)
            ?? throw new KeyNotFoundException($"Command with Id {commandId} not found.");

        // Cerca stato esistente
        var existingState = await _deviceStateRepository.GetByCommandAndDeviceAsync(commandId, deviceType, ct);

        if (existingState is not null)
        {
            existingState.IsEnabled = isEnabled;
            await _deviceStateRepository.UpdateAsync(existingState, ct);
        }
        else
        {
            var newState = new CommandDeviceStateEntity
            {
                CommandId = commandId,
                DeviceType = deviceType,
                IsEnabled = isEnabled
            };
            await _deviceStateRepository.AddAsync(newState, ct);
        }
    }

    public async Task<CommandDeviceState?> GetDeviceStateAsync(int commandId, DeviceType deviceType,
        CancellationToken ct = default)
    {
        var entity = await _deviceStateRepository.GetByCommandAndDeviceAsync(commandId, deviceType, ct);

        return entity is null ? null : CommandDeviceStateMapper.ToDomain(entity);
    }
}
