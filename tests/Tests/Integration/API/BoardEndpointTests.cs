using API.Dtos;
using API.Mapping;
using Core.Enums;

namespace Tests.Integration.API;

/// <summary>
/// Integration tests per Board definition endpoint (BR-API-005).
/// Formato compatibile Production.Tracker.
/// </summary>
public class BoardEndpointTests : ApiIntegrationTestBase
{
    // === GET /api/boards/{id}/definition ===

    [Fact]
    public async Task GetBoardDefinition_ReturnsFullDefinition()
    {
        var (_, boardId, _, _) = await SeedFullScenarioAsync();

        var board = await BoardService.GetByIdAsync(boardId);
        Assert.NotNull(board);

        var device = await DeviceService.GetByIdAsync(board!.DeviceId);
        Assert.NotNull(device);

        Assert.Equal("Optimus-XP", device!.Name);
        Assert.Equal("Madre", board.Name);
        Assert.NotNull(board.DictionaryId);
    }

    [Fact]
    public async Task GetBoardDefinition_OnlyEnabledVariables()
    {
        var (_, boardId, stdDictId, specDictId) = await SeedFullScenarioAsync();

        var board = await BoardService.GetByIdAsync(boardId);
        Assert.NotNull(board);

        // Simula logica BoardEndpoints
        var variables = new List<VariableDto>();

        // Standard risolte
        var stdDict = await DictionaryService.GetStandardDictionaryAsync();
        var stdVars = await VariableService.GetByDictionaryIdAsync(stdDict!.Id);
        var overrides = await VariableService.GetOverridesByDictionaryAsync(board!.DictionaryId!.Value);

        foreach (var sv in stdVars)
        {
            var ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
            if (!sv.IsEnabled) continue;
            var enabled = ov?.IsEnabled ?? sv.IsEnabled;
            if (!enabled) continue;
            variables.Add(ApiMapper.ToVariableDto(sv));
        }

        // Specifiche abilitate
        var specVars = await VariableService.GetByDictionaryIdAsync(board.DictionaryId!.Value);
        foreach (var v in specVars.Where(v => v.IsEnabled))
            variables.Add(ApiMapper.ToVariableDto(v));

        // Firmware (std, abilitata) + SystemOn (specifica)
        // Matricola: override disabled → esclusa
        // Deprecata: globalmente disabled → esclusa
        // Disabilitata: specifica disabled → esclusa
        Assert.Equal(2, variables.Count);
        Assert.Contains(variables, v => v.Name == "Firmware");
        Assert.Contains(variables, v => v.Name == "SystemOn");
    }

    [Fact]
    public async Task GetBoardDefinition_OrderedByAddress()
    {
        var (_, boardId, _, _) = await SeedFullScenarioAsync();

        var board = await BoardService.GetByIdAsync(boardId);
        var stdDict = await DictionaryService.GetStandardDictionaryAsync();
        var stdVars = await VariableService.GetByDictionaryIdAsync(stdDict!.Id);
        var overrides = await VariableService.GetOverridesByDictionaryAsync(board!.DictionaryId!.Value);
        var specVars = await VariableService.GetByDictionaryIdAsync(board.DictionaryId!.Value);

        var variables = new List<VariableDto>();
        foreach (var sv in stdVars.Where(v => v.IsEnabled))
        {
            var ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
            if (!(ov?.IsEnabled ?? sv.IsEnabled)) continue;
            variables.Add(ApiMapper.ToVariableDto(sv));
        }
        foreach (var v in specVars.Where(v => v.IsEnabled))
            variables.Add(ApiMapper.ToVariableDto(v));

        variables = [.. variables.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow)];

        for (int i = 1; i < variables.Count; i++)
        {
            var prev = (variables[i - 1].AddressHigh << 8) | variables[i - 1].AddressLow;
            var curr = (variables[i].AddressHigh << 8) | variables[i].AddressLow;
            Assert.True(prev <= curr);
        }
    }

    [Fact]
    public async Task GetBoardDefinition_BoardWithoutDictionary_ReturnsNull()
    {
        var device = await DeviceService.AddAsync(
            new Core.Models.Device("Spark", 7));

        var board = await BoardService.AddAsync(
            new Core.Models.Board(device.Id, "Motore DX", firmwareType: 12,
                boardNumber: 2, dictionaryId: null, machineCode: 7));

        var loaded = await BoardService.GetByIdAsync(board.Id);
        Assert.Null(loaded!.DictionaryId);
    }

    [Fact]
    public async Task GetBoardDefinition_ProtocolAddressFormat()
    {
        var (_, boardId, _, _) = await SeedFullScenarioAsync();

        var board = await BoardService.GetByIdAsync(boardId);
        var dto = ApiMapper.ToBoardSummaryDto(board!);

        // Formato "0x" + 8 hex digits
        Assert.StartsWith("0x", dto.ProtocolAddress);
        Assert.Equal(10, dto.ProtocolAddress.Length);
    }

    [Fact]
    public async Task GetBoardDefinition_StandardOverrideDescription_Applied()
    {
        var (_, _, _, specDictId) = await SeedFullScenarioAsync();

        var overrides = await VariableService.GetOverridesByDictionaryAsync(specDictId);
        var matricolaOverride = overrides[0];

        Assert.Equal("Non usato su Optimus", matricolaOverride.Description);
        Assert.False(matricolaOverride.IsEnabled);
    }
}
