using System.Text.Json;
using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Command service implementation.
/// </summary>
public class CommandService : ICommandService
{
    private readonly ICommandRepository _repository;
    private readonly ICommandDeviceStateRepository _deviceStateRepository;
    private readonly IAuditService _audit;
    private readonly ICurrentUserProvider _userProvider;

    public CommandService(
        ICommandRepository repository,
        ICommandDeviceStateRepository deviceStateRepository,
        IAuditService auditService,
        ICurrentUserProvider userProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(deviceStateRepository);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(userProvider);
        _repository = repository;
        _deviceStateRepository = deviceStateRepository;
        _audit = auditService;
        _userProvider = userProvider;
    }

    // === Base CRUD ===

    public async Task<Command?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        CommandEntity? entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : CommandMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<CommandEntity> entities = await _repository.GetAllAsync(ct);
        return CommandMapper.ToDomainList(entities);
    }

    public async Task<Command> AddAsync(Command command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Name uniqueness check
        CommandEntity? existingByName = await _repository.GetByNameAsync(command.Name, ct);
        if (existingByName is not null)
        {
            throw new InvalidOperationException(
                $"Command with name '{command.Name}' already exists.");
        }

        // Code uniqueness check
        CommandEntity? existing = await _repository.GetByCodeAsync(
            command.CodeHigh, command.CodeLow, command.IsResponse, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Command with code 0x{command.CodeHigh:X2}{command.CodeLow:X2} " +
                $"(IsResponse={command.IsResponse}) already exists ('{existing.Name}').");
        }

        CommandEntity entity = CommandMapper.ToEntity(command);
        CommandEntity created = await _repository.AddAsync(entity, ct);
        Command result = CommandMapper.ToDomain(created);

        await _audit.LogCreateAsync(AuditEntityType.Command, result.Id,
            _userProvider.CurrentUserId ?? 0,
            JsonSerializer.Serialize(result), ct: ct);

        return result;
    }

    public async Task UpdateAsync(Command command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        CommandEntity entity = await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Command '{command.Name}' (Id={command.Id}) not found.");

        // Name uniqueness check (if changed)
        if (!entity.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase))
        {
            CommandEntity? existingByName = await _repository.GetByNameAsync(command.Name, ct);
            if (existingByName is not null)
            {
                throw new InvalidOperationException(
                    $"Command with name '{command.Name}' already exists.");
            }
        }

        Command previous = CommandMapper.ToDomain(entity);
        string prevJson = JsonSerializer.Serialize(previous);

        CommandMapper.UpdateEntity(entity, command);
        await _repository.UpdateAsync(entity, ct);

        await _audit.LogUpdateAsync(AuditEntityType.Command, command.Id,
            _userProvider.CurrentUserId ?? 0,
            prevJson, JsonSerializer.Serialize(command), ct: ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        CommandEntity? entity = await _repository.GetByIdAsync(id, ct);
        if (entity is not null)
        {
            Command previous = CommandMapper.ToDomain(entity);
            await _repository.DeleteAsync(id, ct);
            await _audit.LogDeleteAsync(AuditEntityType.Command, id,
                _userProvider.CurrentUserId ?? 0,
                JsonSerializer.Serialize(previous), ct: ct);
        }
        else
        {
            await _repository.DeleteAsync(id, ct);
        }
    }

    // === Specific queries ===

    public async Task<Command?> GetByCodeAsync(byte codeHigh, byte codeLow, bool isResponse,
        CancellationToken ct = default)
    {
        CommandEntity? entity = await _repository.GetByCodeAsync(codeHigh, codeLow, isResponse, ct);
        return entity is null ? null : CommandMapper.ToDomain(entity);
    }

    // === DeviceState management ===

    public async Task<Command?> GetWithDeviceStatesAsync(int id, CancellationToken ct = default)
    {
        CommandEntity? entity = await _repository.GetWithDeviceStatesAsync(id, ct);
        // The mapper carries the eager-loaded states onto Command.DeviceStates.
        return entity is null ? null : CommandMapper.ToDomain(entity);
    }

    public async Task SetDeviceStateAsync(int commandId, int deviceId, bool isEnabled,
        CancellationToken ct = default)
    {
        // Check that the command exists
        CommandEntity cmd = await _repository.GetByIdAsync(commandId, ct)
            ?? throw new KeyNotFoundException(
                $"Command (Id={commandId}) not found.");

        // Look up existing state
        CommandDeviceStateEntity? existingState = await _deviceStateRepository.GetByCommandAndDeviceAsync(commandId, deviceId, ct);

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
                DeviceId = deviceId,
                IsEnabled = isEnabled
            };
            await _deviceStateRepository.AddAsync(newState, ct);
        }
    }

    public async Task<CommandDeviceState?> GetDeviceStateAsync(int commandId, int deviceId,
        CancellationToken ct = default)
    {
        CommandDeviceStateEntity? entity = await _deviceStateRepository.GetByCommandAndDeviceAsync(commandId, deviceId, ct);

        return entity is null ? null : CommandDeviceStateMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<CommandDeviceState>> GetDeviceStatesForDeviceAsync(
        int deviceId, CancellationToken ct = default)
    {
        IReadOnlyList<CommandDeviceStateEntity> entities = await _deviceStateRepository.GetByDeviceIdAsync(deviceId, ct);
        return [.. entities.Select(CommandDeviceStateMapper.ToDomain)];
    }
}
