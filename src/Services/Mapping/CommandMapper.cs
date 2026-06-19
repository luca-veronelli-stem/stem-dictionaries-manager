using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for Command Entity &lt;-&gt; Domain. Parameter
/// (de)serialization is owned by the EF Core typed value conversion on
/// <c>CommandEntity.Parameters</c>, so the mapper only copies the list.
/// </summary>
public static class CommandMapper
{
    /// <summary>
    /// Converts CommandEntity to Command (Domain).
    /// </summary>
    public static Command ToDomain(CommandEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Command.Restore(
            entity.Id,
            entity.Name,
            entity.CodeHigh,
            entity.CodeLow,
            entity.IsResponse,
            entity.Parameters,
            entity.DeviceStates?.Select(CommandDeviceStateMapper.ToDomain));
    }

    /// <summary>
    /// Converts Command (Domain) to CommandEntity for creation.
    /// </summary>
    public static CommandEntity ToEntity(Command domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CommandEntity
        {
            Id = domain.Id,
            Name = domain.Name,
            CodeHigh = domain.CodeHigh,
            CodeLow = domain.CodeLow,
            IsResponse = domain.IsResponse,
            Parameters = [.. domain.Parameters]
        };
    }

    /// <summary>
    /// Updates an existing CommandEntity with data from Command (Domain).
    /// </summary>
    public static void UpdateEntity(CommandEntity entity, Command domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Name = domain.Name;
        entity.CodeHigh = domain.CodeHigh;
        entity.CodeLow = domain.CodeLow;
        entity.IsResponse = domain.IsResponse;
        entity.Parameters = [.. domain.Parameters];
    }

    /// <summary>
    /// Converts a list of entities to a list of domain models.
    /// </summary>
    public static IReadOnlyList<Command> ToDomainList(IEnumerable<CommandEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
