using API.Dtos;
using API.Mapping;
using Core.Models;
using Services.Interfaces;

namespace API.Endpoints;

/// <summary>
/// Endpoint board definition: GET /api/boards/{id}/definition (BR-API-005).
/// </summary>
public static class BoardEndpoints
{
    public static void MapBoardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/boards")
            .WithTags("Schede");

        group.MapGet("/{id:int}/definition", GetDefinition)
            .WithName("GetBoardDefinition");
    }

    /// <summary>
    /// Board definition — formato compatibile Production.Tracker.
    /// Variabili risolte (standard con override + specifiche), solo abilitate.
    /// </summary>
    private static async Task<IResult> GetDefinition(
        int id, IBoardService boardService, IDeviceService deviceService,
        IDictionaryService dictionaryService, IVariableService variableService,
        CancellationToken ct)
    {
        var board = await boardService.GetByIdAsync(id, ct);
        if (board is null)
            return Results.NotFound();

        var device = await deviceService.GetByIdAsync(board.DeviceId, ct);
        if (device is null)
            return Results.NotFound();

        if (board.DictionaryId is null)
            return Results.NotFound(new { error = "La scheda non ha un dizionario associato." });

        var dictionary = await dictionaryService.GetByIdAsync(board.DictionaryId.Value, ct);
        if (dictionary is null)
            return Results.NotFound();

        var variables = new List<VariableDto>();

        // 1. Variabili standard risolte (con override BR-009/020)
        var standardDict = await dictionaryService.GetStandardDictionaryAsync(ct);
        if (standardDict is not null)
        {
            var standardVars = await variableService.GetByDictionaryIdAsync(
                standardDict.Id, ct);
            var overrides = await variableService.GetOverridesByDictionaryAsync(
                board.DictionaryId.Value, ct);

            foreach (var sv in standardVars)
            {
                var ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
                var effectiveEnabled = ResolveEnabled(sv, ov);
                if (!effectiveEnabled) continue;

                var effectiveDescription = ResolveDescription(sv, ov);
                variables.Add(new VariableDto(
                    Name: sv.Name,
                    AddressHigh: sv.AddressHigh,
                    AddressLow: sv.AddressLow,
                    DataType: sv.DataTypeRaw,
                    Access: sv.AccessMode.ToString(),
                    Description: effectiveDescription,
                    Min: sv.MinValue,
                    Max: sv.MaxValue,
                    Unit: sv.Unit));
            }
        }

        // 2. Variabili specifiche del dizionario (solo abilitate)
        var specificVars = await variableService.GetByDictionaryIdAsync(
            board.DictionaryId.Value, ct);
        foreach (var v in specificVars.Where(v => v.IsEnabled))
            variables.Add(ApiMapper.ToVariableDto(v));

        // Ordina per indirizzo
        variables = variables.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow).ToList();

        var dto = new BoardDefinitionDto(
            DeviceName: device.Name,
            BoardName: board.Name,
            BoardAddress: $"0x{board.ProtocolAddress:X8}",
            FirmwareType: board.FirmwareType,
            Description: device.Description,
            Variables: variables);

        return Results.Ok(dto);
    }

    private static bool ResolveEnabled(Variable variable, StandardVariableOverride? ov)
    {
        if (!variable.IsEnabled) return false;
        return ov?.IsEnabled ?? variable.IsEnabled;
    }

    private static string? ResolveDescription(Variable variable, StandardVariableOverride? ov)
    {
        if (ov?.Description is not null) return ov.Description;
        return variable.Description;
    }
}
