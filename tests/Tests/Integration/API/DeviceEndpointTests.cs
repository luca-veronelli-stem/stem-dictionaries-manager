using API.Dtos;
using API.Endpoints;
using API.Mapping;
using Core.Models;

namespace Tests.Integration.API;

/// <summary>
/// Integration tests per Device endpoints.
/// Testa la logica degli endpoint usando services reali + DB in-memory.
/// </summary>
public class DeviceEndpointTests : ApiIntegrationTestBase
{
    // === GET /api/devices ===

    [Fact]
    public async Task GetDevices_Empty_ReturnsEmptyList()
    {
        IReadOnlyList<Device> devices = await DeviceService.GetAllAsync();
        var dtos = devices.Select(ApiMapper.ToDeviceSummaryDto).ToList();

        Assert.Empty(dtos);
    }

    [Fact]
    public async Task GetDevices_WithData_ReturnsAllDevices()
    {
        await DeviceService.AddAsync(new Core.Models.Device("Optimus-XP", 10));
        await DeviceService.AddAsync(new Core.Models.Device("Spark", 7));

        IReadOnlyList<Device> devices = await DeviceService.GetAllAsync();
        var dtos = devices.Select(ApiMapper.ToDeviceSummaryDto).ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Contains(dtos, d => d.Name == "Optimus-XP");
        Assert.Contains(dtos, d => d.Name == "Spark");
    }

    [Fact]
    public async Task GetDevices_MapsProperties()
    {
        await DeviceService.AddAsync(
            new Core.Models.Device("Eden-XP", 3, "Piano di trattamento"));

        IReadOnlyList<Device> devices = await DeviceService.GetAllAsync();
        DeviceSummaryDto dto = devices.Select(ApiMapper.ToDeviceSummaryDto).First();

        Assert.Equal("Eden-XP", dto.Name);
        Assert.Equal(3, dto.MachineCode);
        Assert.Equal("Piano di trattamento", dto.Description);
    }

    // === GET /api/devices/{id} ===

    [Fact]
    public async Task GetDevice_NotFound_ReturnsNull()
    {
        Device? device = await DeviceService.GetByIdAsync(999);

        Assert.Null(device);
    }

    [Fact]
    public async Task GetDevice_WithBoards_ReturnsDeviceWithBoards()
    {
        (int deviceId, int _, int _, int _) = await SeedFullScenarioAsync();

        Device? device = await DeviceService.GetByIdAsync(deviceId);
        IReadOnlyList<Board> boards = await BoardService.GetByDeviceIdAsync(deviceId);

        Assert.NotNull(device);
        Assert.Equal("Optimus-XP", device!.Name);
        Assert.Single(boards);
        Assert.Equal("Madre", boards[0].Name);
    }

    // === GET /api/devices/{id}/boards ===

    [Fact]
    public async Task GetDeviceBoards_ReturnsBoards()
    {
        (int deviceId, int _, int _, int _) = await SeedFullScenarioAsync();

        IReadOnlyList<Board> boards = await BoardService.GetByDeviceIdAsync(deviceId);
        var dtos = boards.Select(ApiMapper.ToBoardSummaryDto).ToList();

        Assert.Single(dtos);
        BoardSummaryDto dto = dtos[0];
        Assert.Equal("Madre", dto.Name);
        Assert.True(dto.IsPrimary);
        Assert.Equal(5, dto.FirmwareType);
        Assert.StartsWith("0x", dto.ProtocolAddress);
    }

    [Fact]
    public async Task GetDeviceBoards_NoBoards_ReturnsEmpty()
    {
        Device device = await DeviceService.AddAsync(
            new Core.Models.Device("Solo", 15));

        IReadOnlyList<Board> boards = await BoardService.GetByDeviceIdAsync(device.Id);

        Assert.Empty(boards);
    }
}
