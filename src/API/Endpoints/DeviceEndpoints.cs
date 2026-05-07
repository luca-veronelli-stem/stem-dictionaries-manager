using API.Dtos;
using API.Mapping;
using Core.Models;
using Services.Interfaces;

namespace API.Endpoints;

/// <summary>
/// Device endpoints: GET /api/devices, GET /api/devices/{id}, GET /api/devices/{id}/boards
/// </summary>
public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/devices")
            .WithTags("Devices");

        group.MapGet("/", GetAll).WithName("GetDevices");
        group.MapGet("/{id:int}", GetById).WithName("GetDevice");
        group.MapGet("/{id:int}/boards", GetBoards).WithName("GetDeviceBoards");
    }

    private static async Task<IResult> GetAll(
        IDeviceService deviceService, CancellationToken ct)
    {
        IReadOnlyList<Device> devices = await deviceService.GetAllAsync(ct);
        var dtos = devices.Select(ApiMapper.ToDeviceSummaryDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetById(
        int id, IDeviceService deviceService, IBoardService boardService,
        CancellationToken ct)
    {
        Device? device = await deviceService.GetByIdAsync(id, ct);
        if (device is null)
        {
            return Results.NotFound();
        }

        IReadOnlyList<Board> boards = await boardService.GetByDeviceIdAsync(device.Id, ct);
        var dto = new DeviceDetailDto(
            Id: device.Id,
            Name: device.Name,
            MachineCode: device.MachineCode,
            Description: device.Description,
            Boards: [.. boards.Select(ApiMapper.ToBoardSummaryDto)]);

        return Results.Ok(dto);
    }

    private static async Task<IResult> GetBoards(
        int id, IDeviceService deviceService, IBoardService boardService,
        CancellationToken ct)
    {
        Device? device = await deviceService.GetByIdAsync(id, ct);
        if (device is null)
        {
            return Results.NotFound();
        }

        IReadOnlyList<Board> boards = await boardService.GetByDeviceIdAsync(device.Id, ct);
        var dtos = boards.Select(ApiMapper.ToBoardSummaryDto).ToList();
        return Results.Ok(dtos);
    }
}
