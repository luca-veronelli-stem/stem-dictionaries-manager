using API.Dtos;
using API.Mapping;
using Core.Models;
using Services.Interfaces;

namespace API.Endpoints;

/// <summary>
/// Board definition endpoints: GET /api/boards/{id}/definition (BR-API-005).
/// </summary>
public static class BoardEndpoints
{
    public static void MapBoardEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/boards")
            .WithTags("Boards");

        group.MapGet("/{id:int}/definition", GetDefinition)
            .WithName("GetBoardDefinition")
            .Produces<BoardDefinitionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesAuthAndDbErrors();
    }

    /// <summary>
    /// Board definition — Production.Tracker-compatible format.
    /// Resolved variables (standard with overrides + device-specific), enabled only.
    /// </summary>
    private static async Task<IResult> GetDefinition(
        int id, IBoardService boardService, IDeviceService deviceService,
        IDictionaryService dictionaryService, IVariableService variableService,
        CancellationToken ct)
    {
        Board? board = await boardService.GetByIdAsync(id, ct);
        if (board is null)
        {
            return Results.NotFound();
        }

        Device? device = await deviceService.GetByIdAsync(board.DeviceId, ct);
        if (device is null)
        {
            return Results.NotFound();
        }

        if (board.DictionaryId is null)
        {
            return Results.NotFound(new { error = "The board has no associated dictionary." });
        }

        Dictionary? dictionary = await dictionaryService.GetByIdAsync(board.DictionaryId.Value, ct);
        if (dictionary is null)
        {
            return Results.NotFound();
        }

        var variables = new List<VariableDto>();

        // 1. Resolved standard variables (with overrides BR-009/020)
        Dictionary? standardDict = await dictionaryService.GetStandardDictionaryAsync(ct);
        if (standardDict is not null)
        {
            IReadOnlyList<Variable> standardVars = await variableService.GetByDictionaryIdAsync(
                standardDict.Id, ct);
            IReadOnlyList<StandardVariableOverride> overrides = await variableService.GetOverridesByDictionaryAsync(
                board.DictionaryId.Value, ct);

            foreach (Variable sv in standardVars)
            {
                StandardVariableOverride? ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
                bool effectiveEnabled = ResolveEnabled(sv, ov);
                if (!effectiveEnabled)
                {
                    continue;
                }

                string? effectiveDescription = ResolveDescription(sv, ov);
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

        // 2. Dictionary-specific variables (enabled only)
        IReadOnlyList<Variable> specificVars = await variableService.GetByDictionaryIdAsync(
            board.DictionaryId.Value, ct);
        foreach (Variable? v in specificVars.Where(v => v.IsEnabled))
        {
            variables.Add(ApiMapper.ToVariableDto(v));
        }

        // Sort by address
        variables = [.. variables.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow)];

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
        if (!variable.IsEnabled)
        {
            return false;
        }

        return ov?.IsEnabled ?? variable.IsEnabled;
    }

    private static string? ResolveDescription(Variable variable, StandardVariableOverride? ov)
    {
        if (ov?.Description is not null)
        {
            return ov.Description;
        }

        return variable.Description;
    }
}
