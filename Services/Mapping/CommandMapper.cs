using System.Text.Json;
using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per Command Entity ↔ Domain.
/// Gestisce la conversione JSON dei parametri.
/// </summary>
public static class CommandMapper
{
    /// <summary>
    /// Converte CommandEntity in Command (Domain).
    /// </summary>
    public static Command ToDomain(CommandEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var parameters = DeserializeParameters(entity.ParametersJson);

        return Command.Restore(
            entity.Id,
            entity.Name,
            entity.CodeHigh,
            entity.CodeLow,
            entity.IsResponse,
            parameters);
    }

    /// <summary>
    /// Converte Command (Domain) in CommandEntity per creazione.
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
    /// Aggiorna CommandEntity esistente con dati da Command (Domain).
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
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<Command> ToDomainList(IEnumerable<CommandEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }

    // === Private Helpers ===

    private static IReadOnlyList<string> DeserializeParameters(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

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
            return "[]";

        return JsonSerializer.Serialize(parameters);
    }
}
