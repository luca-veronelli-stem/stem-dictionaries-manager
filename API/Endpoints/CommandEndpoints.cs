using API.Mapping;
using Services.Interfaces;

namespace API.Endpoints;

/// <summary>
/// Endpoint comandi: GET /api/commands, GET /api/commands/device/{deviceId}
/// </summary>
public static class CommandEndpoints
{
    public static void MapCommandEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/commands")
            .WithTags("Comandi");

        group.MapGet("/", GetAll).WithName("GetCommands");
        group.MapGet("/device/{deviceId:int}", GetByDevice).WithName("GetDeviceCommands");
    }

    private static async Task<IResult> GetAll(
        ICommandService commandService, CancellationToken ct)
    {
        var commands = await commandService.GetAllAsync(ct);
        var dtos = commands.Select(ApiMapper.ToCommandDto).ToList();
        return Results.Ok(dtos);
    }

    /// <summary>
    /// Comandi abilitati per device (BR-API-003: default=true, solo abilitati).
    /// </summary>
    private static async Task<IResult> GetByDevice(
        int deviceId, IDeviceService deviceService,
        ICommandService commandService, CancellationToken ct)
    {
        var device = await deviceService.GetByIdAsync(deviceId, ct);
        if (device is null)
            return Results.NotFound();

        var commands = await commandService.GetAllAsync(ct);
        var states = await commandService.GetDeviceStatesForDeviceAsync(deviceId, ct);

        var enabledCommands = commands.Where(c =>
        {
            var state = states.FirstOrDefault(s => s.CommandId == c.Id);
            return state?.IsEnabled ?? true;
        });

        var dtos = enabledCommands.Select(ApiMapper.ToCommandDeviceDto).ToList();
        return Results.Ok(dtos);
    }
}
