using System.Text.Json;
using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for Command Entity ↔ Domain.
/// Handles JSON conversion of parameters.
/// </summary>
public static class CommandMapper
{
    /// <summary>
    /// Converts CommandEntity to Command (Domain).
    /// </summary>
    public static Command ToDomain(CommandEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        IReadOnlyList<string> parameters = DeserializeParameters(entity.ParametersJson);

        return Command.Restore(
            entity.Id,
            entity.Name,
            entity.CodeHigh,
            entity.CodeLow,
            entity.IsResponse,
            parameters);
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
            ParametersJson = SerializeParameters(domain.Parameters)
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
        entity.ParametersJson = SerializeParameters(domain.Parameters);
    }

    /// <summary>
    /// Converts a list of entities to a list of domain models.
    /// </summary>
    public static IReadOnlyList<Command> ToDomainList(IEnumerable<CommandEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }

    // === Private helpers ===

    private static IReadOnlyList<string> DeserializeParameters(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeParameters(IReadOnlyList<string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return "[]";
        }

        return JsonSerializer.Serialize(parameters);
    }
}
