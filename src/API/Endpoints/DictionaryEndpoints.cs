using API.Dtos;
using API.Mapping;
using Core.Models;
using Services.Interfaces;

namespace API.Endpoints;

/// <summary>
/// Dictionary endpoints: GET /api/dictionaries, /standard, /{id}, /{id}/resolved
/// </summary>
public static class DictionaryEndpoints
{
    public static void MapDictionaryEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/dictionaries")
            .WithTags("Dictionaries");

        group.MapGet("/", GetAll).WithName("GetDictionaries")
            .Produces<DictionarySummaryDto[]>(StatusCodes.Status200OK)
            .ProducesAuthAndDbErrors();
        group.MapGet("/standard", GetStandard).WithName("GetStandardDictionary")
            .Produces<DictionaryDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesAuthAndDbErrors();
        group.MapGet("/{id:int}", GetById).WithName("GetDictionary")
            .Produces<DictionaryDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesAuthAndDbErrors();
        group.MapGet("/{id:int}/resolved", GetResolved).WithName("GetDictionaryResolved")
            .Produces<DictionaryResolvedDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesAuthAndDbErrors();
    }

    private static async Task<IResult> GetAll(
        IDictionaryService dictionaryService, IVariableService variableService,
        CancellationToken ct)
    {
        IReadOnlyList<Dictionary> dictionaries = await dictionaryService.GetAllAsync(ct);
        var dtos = new List<DictionarySummaryDto>();

        foreach (Dictionary d in dictionaries)
        {
            IReadOnlyList<Variable> vars = await variableService.GetByDictionaryIdAsync(d.Id, ct);
            int enabledCount = vars.Count(v => v.IsEnabled);
            dtos.Add(ApiMapper.ToDictionarySummaryDto(d, enabledCount));
        }

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetStandard(
        IDictionaryService dictionaryService, IVariableService variableService,
        CancellationToken ct)
    {
        Dictionary? standard = await dictionaryService.GetStandardDictionaryAsync(ct);
        if (standard is null)
        {
            return Results.NotFound();
        }

        IReadOnlyList<Variable> vars = await variableService.GetByDictionaryIdAsync(standard.Id, ct);
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
        Dictionary? dictionary = await dictionaryService.GetByIdAsync(id, ct);
        if (dictionary is null)
        {
            return Results.NotFound();
        }

        IReadOnlyList<Variable> vars = await variableService.GetByDictionaryIdAsync(dictionary.Id, ct);
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
    /// Resolved variables: standard (with overrides BR-009/020) + device-specific, enabled only.
    /// </summary>
    private static async Task<IResult> GetResolved(
        int id, IDictionaryService dictionaryService,
        IVariableService variableService, CancellationToken ct)
    {
        Dictionary? dictionary = await dictionaryService.GetByIdAsync(id, ct);
        if (dictionary is null)
        {
            return Results.NotFound();
        }

        // If standard, return its variables directly
        if (dictionary.IsStandard)
        {
            return Results.BadRequest(new { error = "Use /api/dictionaries/standard for the standard dictionary." });
        }

        var resolved = new List<ResolvedVariableDto>();

        // 1. Resolved standard variables (with overrides BR-009/020)
        Dictionary? standardDict = await dictionaryService.GetStandardDictionaryAsync(ct);
        if (standardDict is not null)
        {
            IReadOnlyList<Variable> standardVars = await variableService.GetByDictionaryIdAsync(standardDict.Id, ct);
            IReadOnlyList<StandardVariableOverride> overrides = await variableService.GetOverridesByDictionaryAsync(id, ct);

            foreach (Variable sv in standardVars)
            {
                StandardVariableOverride? ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
                bool effectiveEnabled = ResolveEnabled(sv, ov);
                if (!effectiveEnabled)
                {
                    continue;
                }

                string? effectiveDescription = ResolveDescription(sv, ov);
                resolved.Add(ApiMapper.ToResolvedDto(sv, isStandard: true,
                    overrideDescription: effectiveDescription));
            }
        }

        // 2. Dictionary-specific variables (enabled only)
        IReadOnlyList<Variable> specificVars = await variableService.GetByDictionaryIdAsync(dictionary.Id, ct);
        foreach (Variable? v in specificVars.Where(v => v.IsEnabled))
        {
            resolved.Add(ApiMapper.ToResolvedDto(v, isStandard: false));
        }

        // Sort by full address
        resolved = [.. resolved.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow)];

        var dto = new DictionaryResolvedDto(
            Id: dictionary.Id,
            Name: dictionary.Name,
            Description: dictionary.Description,
            Variables: resolved);

        return Results.Ok(dto);
    }

    /// <summary>BR-009: effective enabled state of a standard variable for a dictionary.</summary>
    private static bool ResolveEnabled(Variable variable, StandardVariableOverride? ov)
    {
        if (!variable.IsEnabled)
        {
            return false;
        }

        return ov?.IsEnabled ?? variable.IsEnabled;
    }

    /// <summary>BR-020: effective description.</summary>
    private static string? ResolveDescription(Variable variable, StandardVariableOverride? ov)
    {
        if (ov?.Description is not null)
        {
            return ov.Description;
        }

        return variable.Description;
    }
}
