using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione comandi.
/// </summary>
public class CommandService : ICommandService
{
    private readonly ICommandRepository _repository;
    private readonly Infrastructure.AppDbContext _context;

    public CommandService(ICommandRepository repository, Infrastructure.AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);
        _repository = repository;
        _context = context;
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
        var command = await _repository.GetByIdAsync(commandId, ct)
            ?? throw new KeyNotFoundException($"Command with Id {commandId} not found.");
        
        // Cerca stato esistente
        var existingState = await _context.CommandDeviceStates
            .FirstOrDefaultAsync(s => s.CommandId == commandId && s.DeviceType == deviceType, ct);
        
        if (existingState is not null)
        {
            existingState.IsEnabled = isEnabled;
            await _context.SaveChangesAsync(ct);
        }
        else
        {
            var newState = new CommandDeviceStateEntity
            {
                CommandId = commandId,
                DeviceType = deviceType,
                IsEnabled = isEnabled
            };
            _context.CommandDeviceStates.Add(newState);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<CommandDeviceState?> GetDeviceStateAsync(int commandId, DeviceType deviceType, 
        CancellationToken ct = default)
    {
        var entity = await _context.CommandDeviceStates
            .FirstOrDefaultAsync(s => s.CommandId == commandId && s.DeviceType == deviceType, ct);
        
        return entity is null ? null : CommandDeviceStateMapper.ToDomain(entity);
    }
}
