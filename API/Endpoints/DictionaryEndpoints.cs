using API.Dtos;
using API.Mapping;
using Core.Models;
using Services.Interfaces;

namespace API.Endpoints;

/// <summary>
/// Endpoint dizionari: GET /api/dictionaries, /standard, /{id}, /{id}/resolved
/// </summary>
public static class DictionaryEndpoints
{
    public static void MapDictionaryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dictionaries")
            .WithTags("Dizionari");

        group.MapGet("/", GetAll).WithName("GetDictionaries");
        group.MapGet("/standard", GetStandard).WithName("GetStandardDictionary");
        group.MapGet("/{id:int}", GetById).WithName("GetDictionary");
        group.MapGet("/{id:int}/resolved", GetResolved).WithName("GetDictionaryResolved");
    }

    private static async Task<IResult> GetAll(
        IDictionaryService dictionaryService, IVariableService variableService,
        CancellationToken ct)
    {
        var dictionaries = await dictionaryService.GetAllAsync(ct);
        var dtos = new List<DictionarySummaryDto>();

        foreach (var d in dictionaries)
        {
            var vars = await variableService.GetByDictionaryIdAsync(d.Id, ct);
            var enabledCount = vars.Count(v => v.IsEnabled);
            dtos.Add(ApiMapper.ToDictionarySummaryDto(d, enabledCount));
        }

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetStandard(
        IDictionaryService dictionaryService, IVariableService variableService,
        CancellationToken ct)
    {
        var standard = await dictionaryService.GetStandardDictionaryAsync(ct);
        if (standard is null)
            return Results.NotFound();

        var vars = await variableService.GetByDictionaryIdAsync(standard.Id, ct);
        var enabledVars = vars.Where(v => v.IsEnabled)
            .OrderBy(v => v.FullAddress)
            .Select(ApiMapper.ToVariableDto)
            .ToList();

        var dto = new DictionaryDetailDto(
            Id: standard.Id,
            Name: standard.Name,
            Description: standard.Description,
            IsStandard: true,
            Variables: enabledVars);

        return Results.Ok(dto);
    }

    private static async Task<IResult> GetById(
        int id, IDictionaryService dictionaryService,
        IVariableService variableService, CancellationToken ct)
    {
        var dictionary = await dictionaryService.GetByIdAsync(id, ct);
        if (dictionary is null)
            return Results.NotFound();

        var vars = await variableService.GetByDictionaryIdAsync(dictionary.Id, ct);
        var enabledVars = vars.Where(v => v.IsEnabled)
            .OrderBy(v => v.FullAddress)
            .Select(ApiMapper.ToVariableDto)
            .ToList();

        var dto = new DictionaryDetailDto(
            Id: dictionary.Id,
            Name: dictionary.Name,
            Description: dictionary.Description,
            IsStandard: dictionary.IsStandard,
            Variables: enabledVars);

        return Results.Ok(dto);
    }

    /// <summary>
    /// Variabili risolte: standard (con override BR-009/020) + specifiche, solo abilitate.
    /// </summary>
    private static async Task<IResult> GetResolved(
        int id, IDictionaryService dictionaryService,
        IVariableService variableService, CancellationToken ct)
    {
        var dictionary = await dictionaryService.GetByIdAsync(id, ct);
        if (dictionary is null)
            return Results.NotFound();

        // Se è standard, restituisce le sue variabili direttamente
        if (dictionary.IsStandard)
            return Results.BadRequest(new { error = "Usa /api/dictionaries/standard per il dizionario standard." });

        var resolved = new List<ResolvedVariableDto>();

        // 1. Variabili standard risolte (con override BR-009/020)
        var standardDict = await dictionaryService.GetStandardDictionaryAsync(ct);
        if (standardDict is not null)
        {
            var standardVars = await variableService.GetByDictionaryIdAsync(standardDict.Id, ct);
            var overrides = await variableService.GetOverridesByDictionaryAsync(id, ct);

            foreach (var sv in standardVars)
            {
                var ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
                var effectiveEnabled = ResolveEnabled(sv, ov);
                if (!effectiveEnabled) continue;

                var effectiveDescription = ResolveDescription(sv, ov);
                resolved.Add(ApiMapper.ToResolvedDto(sv, isStandard: true,
                    overrideDescription: effectiveDescription));
            }
        }

        // 2. Variabili specifiche del dizionario (solo abilitate)
        var specificVars = await variableService.GetByDictionaryIdAsync(dictionary.Id, ct);
        foreach (var v in specificVars.Where(v => v.IsEnabled))
            resolved.Add(ApiMapper.ToResolvedDto(v, isStandard: false));

        // Ordina per indirizzo completo
        resolved = [.. resolved.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow)];

        var dto = new DictionaryResolvedDto(
            Id: dictionary.Id,
            Name: dictionary.Name,
            Description: dictionary.Description,
            Variables: resolved);

        return Results.Ok(dto);
    }

    /// <summary>BR-009: stato effettivo variabile standard per un dizionario.</summary>
    private static bool ResolveEnabled(Variable variable, StandardVariableOverride? ov)
    {
        if (!variable.IsEnabled) return false;
        return ov?.IsEnabled ?? variable.IsEnabled;
    }

    /// <summary>BR-020: descrizione effettiva.</summary>
    private static string? ResolveDescription(Variable variable, StandardVariableOverride? ov)
    {
        if (ov?.Description is not null) return ov.Description;
        return variable.Description;
    }
}
