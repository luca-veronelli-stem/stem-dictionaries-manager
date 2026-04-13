using API.Mapping;

namespace Tests.Integration.API;

/// <summary>
/// Integration tests per Command endpoints.
/// Copre: lista comandi, comandi abilitati per device (BR-API-003).
/// </summary>
public class CommandEndpointTests : ApiIntegrationTestBase
{
    // === GET /api/commands ===

    [Fact]
    public async Task GetCommands_Empty_ReturnsEmptyList()
    {
        var commands = await CommandService.GetAllAsync();

        Assert.Empty(commands);
    }

    [Fact]
    public async Task GetCommands_ReturnsAll()
    {
        await CommandService.AddAsync(
            new Core.Models.Command("Read Var", 0x00, 0x01, false, ["4|size"]));
        await CommandService.AddAsync(
            new Core.Models.Command("Read Var Resp", 0x80, 0x01, true, ["4|data"]));

        var commands = await CommandService.GetAllAsync();
        var dtos = commands.Select(ApiMapper.ToCommandDto).ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Contains(dtos, c => c.Name == "Read Var" && !c.IsResponse);
        Assert.Contains(dtos, c => c.Name == "Read Var Resp" && c.IsResponse);
    }

    [Fact]
    public async Task GetCommands_MapsParametersCorrectly()
    {
        await CommandService.AddAsync(
            new Core.Models.Command("Write Var", 0x00, 0x02, false, ["4|size", "2|addr"]));

        var commands = await CommandService.GetAllAsync();
        var dto = commands.Select(ApiMapper.ToCommandDto).First();

        Assert.Equal(2, dto.Parameters.Count);
    }

    // === GET /api/commands/device/{deviceId} ===

    [Fact]
    public async Task GetDeviceCommands_DefaultEnabled_ReturnsAll()
    {
        var device = await DeviceService.AddAsync(
            new Core.Models.Device("Optimus-XP", 10));

        var cmd = await CommandService.AddAsync(
            new Core.Models.Command("Read Var", 0x00, 0x01));

        // Nessun override → default enabled=true
        var commands = await CommandService.GetAllAsync();
        var states = await CommandService.GetDeviceStatesForDeviceAsync(device.Id);

        var enabled = commands.Where(c =>
        {
            var state = states.FirstOrDefault(s => s.CommandId == c.Id);
            return state?.IsEnabled ?? true;
        }).Select(ApiMapper.ToCommandDeviceDto).ToList();

        Assert.Single(enabled);
        Assert.Equal("Read Var", enabled[0].Name);
    }

    [Fact]
    public async Task GetDeviceCommands_DisabledCommand_Excluded()
    {
        var device = await DeviceService.AddAsync(
            new Core.Models.Device("Optimus-XP", 10));

        var cmd1 = await CommandService.AddAsync(
            new Core.Models.Command("Read Var", 0x00, 0x01));
        var cmd2 = await CommandService.AddAsync(
            new Core.Models.Command("Write Var", 0x00, 0x02));

        // Disabilita cmd1 per questo device
        await CommandService.SetDeviceStateAsync(cmd1.Id, device.Id, isEnabled: false);

        var commands = await CommandService.GetAllAsync();
        var states = await CommandService.GetDeviceStatesForDeviceAsync(device.Id);

        var enabled = commands.Where(c =>
        {
            var state = states.FirstOrDefault(s => s.CommandId == c.Id);
            return state?.IsEnabled ?? true;
        }).Select(ApiMapper.ToCommandDeviceDto).ToList();

        Assert.Single(enabled);
        Assert.Equal("Write Var", enabled[0].Name);
    }

    [Fact]
    public async Task GetDeviceCommands_OverrideForOtherDevice_Ignored()
    {
        var device1 = await DeviceService.AddAsync(new Core.Models.Device("Dev1", 10));
        var device2 = await DeviceService.AddAsync(new Core.Models.Device("Dev2", 11));

        var cmd = await CommandService.AddAsync(
            new Core.Models.Command("Read Var", 0x00, 0x01));

        // Disabilita solo per device2
        await CommandService.SetDeviceStateAsync(cmd.Id, device2.Id, isEnabled: false);

        // Per device1 deve essere ancora abilitato
        var states = await CommandService.GetDeviceStatesForDeviceAsync(device1.Id);
        var commands = await CommandService.GetAllAsync();
        var enabled = commands.Where(c =>
        {
            var state = states.FirstOrDefault(s => s.CommandId == c.Id);
            return state?.IsEnabled ?? true;
        }).ToList();

        Assert.Single(enabled);
    }
}
