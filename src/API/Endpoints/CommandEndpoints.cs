using API.Mapping;
using Core.Models;
using Services.Interfaces;

namespace API.Endpoints;

/// <summary>
/// Command endpoints: GET /api/commands, GET /api/commands/device/{deviceId}
/// </summary>
public static class CommandEndpoints
{
    public static void MapCommandEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/commands")
            .WithTags("Commands");

        group.MapGet("/", GetAll).WithName("GetCommands");
        group.MapGet("/device/{deviceId:int}", GetByDevice).WithName("GetDeviceCommands");
    }

    private static async Task<IResult> GetAll(
        ICommandService commandService, CancellationToken ct)
    {
        IReadOnlyList<Command> commands = await commandService.GetAllAsync(ct);
        var dtos = commands.Select(ApiMapper.ToCommandDto).ToList();
        return Results.Ok(dtos);
    }

    /// <summary>
    /// Commands enabled for a device (BR-API-003: default=true, enabled only).
    /// </summary>
    private static async Task<IResult> GetByDevice(
        int deviceId, IDeviceService deviceService,
        ICommandService commandService, CancellationToken ct)
    {
        Device? device = await deviceService.GetByIdAsync(deviceId, ct);
        if (device is null)
        {
            return Results.NotFound();
        }

        IReadOnlyList<Command> commands = await commandService.GetAllAsync(ct);
        IReadOnlyList<CommandDeviceState> states = await commandService.GetDeviceStatesForDeviceAsync(deviceId, ct);

        IEnumerable<Command> enabledCommands = commands.Where(c =>
        {
            CommandDeviceState? state = states.FirstOrDefault(s => s.CommandId == c.Id);
            return state?.IsEnabled ?? true;
        });

        var dtos = enabledCommands.Select(ApiMapper.ToCommandDeviceDto).ToList();
        return Results.Ok(dtos);
    }
}
