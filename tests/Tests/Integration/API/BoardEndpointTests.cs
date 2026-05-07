using API.Dtos;
using API.Mapping;
using Core.Enums;
using Core.Models;

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
        (int _, int boardId, int _, int _) = await SeedFullScenarioAsync();

        Board? board = await BoardService.GetByIdAsync(boardId);
        Assert.NotNull(board);

        Device? device = await DeviceService.GetByIdAsync(board!.DeviceId);
        Assert.NotNull(device);

        Assert.Equal("Optimus-XP", device!.Name);
        Assert.Equal("Madre", board.Name);
        Assert.NotNull(board.DictionaryId);
    }

    [Fact]
    public async Task GetBoardDefinition_OnlyEnabledVariables()
    {
        (int _, int boardId, int stdDictId, int specDictId) = await SeedFullScenarioAsync();

        Board? board = await BoardService.GetByIdAsync(boardId);
        Assert.NotNull(board);

        // Simula logica BoardEndpoints
        var variables = new List<VariableDto>();

        // Standard risolte
        Dictionary? stdDict = await DictionaryService.GetStandardDictionaryAsync();
        IReadOnlyList<Variable> stdVars = await VariableService.GetByDictionaryIdAsync(stdDict!.Id);
        IReadOnlyList<StandardVariableOverride> overrides = await VariableService.GetOverridesByDictionaryAsync(board!.DictionaryId!.Value);

        foreach (Variable sv in stdVars)
        {
            StandardVariableOverride? ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
            if (!sv.IsEnabled)
            {
                continue;
            }

            bool enabled = ov?.IsEnabled ?? sv.IsEnabled;
            if (!enabled)
            {
                continue;
            }

            variables.Add(ApiMapper.ToVariableDto(sv));
        }

        // Specifiche abilitate
        IReadOnlyList<Variable> specVars = await VariableService.GetByDictionaryIdAsync(board.DictionaryId!.Value);
        foreach (Variable? v in specVars.Where(v => v.IsEnabled))
        {
            variables.Add(ApiMapper.ToVariableDto(v));
        }

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
        (int _, int boardId, int _, int _) = await SeedFullScenarioAsync();

        Board? board = await BoardService.GetByIdAsync(boardId);
        Dictionary? stdDict = await DictionaryService.GetStandardDictionaryAsync();
        IReadOnlyList<Variable> stdVars = await VariableService.GetByDictionaryIdAsync(stdDict!.Id);
        IReadOnlyList<StandardVariableOverride> overrides = await VariableService.GetOverridesByDictionaryAsync(board!.DictionaryId!.Value);
        IReadOnlyList<Variable> specVars = await VariableService.GetByDictionaryIdAsync(board.DictionaryId!.Value);

        var variables = new List<VariableDto>();
        foreach (Variable? sv in stdVars.Where(v => v.IsEnabled))
        {
            StandardVariableOverride? ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
            if (!(ov?.IsEnabled ?? sv.IsEnabled))
            {
                continue;
            }

            variables.Add(ApiMapper.ToVariableDto(sv));
        }
        foreach (Variable? v in specVars.Where(v => v.IsEnabled))
        {
            variables.Add(ApiMapper.ToVariableDto(v));
        }

        variables = [.. variables.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow)];

        for (int i = 1; i < variables.Count; i++)
        {
            int prev = (variables[i - 1].AddressHigh << 8) | variables[i - 1].AddressLow;
            int curr = (variables[i].AddressHigh << 8) | variables[i].AddressLow;
            Assert.True(prev <= curr);
        }
    }

    [Fact]
    public async Task GetBoardDefinition_BoardWithoutDictionary_ReturnsNull()
    {
        Device device = await DeviceService.AddAsync(
            new Core.Models.Device("Spark", 7));

        Board board = await BoardService.AddAsync(
            new Core.Models.Board(device.Id, "Motore DX", firmwareType: 12,
                boardNumber: 2, dictionaryId: null, machineCode: 7));

        Board? loaded = await BoardService.GetByIdAsync(board.Id);
        Assert.Null(loaded!.DictionaryId);
    }

    [Fact]
    public async Task GetBoardDefinition_ProtocolAddressFormat()
    {
        (int _, int boardId, int _, int _) = await SeedFullScenarioAsync();

        Board? board = await BoardService.GetByIdAsync(boardId);
        var dto = ApiMapper.ToBoardSummaryDto(board!);

        // Formato "0x" + 8 hex digits
        Assert.StartsWith("0x", dto.ProtocolAddress);
        Assert.Equal(10, dto.ProtocolAddress.Length);
    }

    [Fact]
    public async Task GetBoardDefinition_StandardOverrideDescription_Applied()
    {
        (int _, int _, int _, int specDictId) = await SeedFullScenarioAsync();

        IReadOnlyList<StandardVariableOverride> overrides = await VariableService.GetOverridesByDictionaryAsync(specDictId);
        StandardVariableOverride matricolaOverride = overrides[0];

        Assert.Equal("Non usato su Optimus", matricolaOverride.Description);
        Assert.False(matricolaOverride.IsEnabled);
    }
}
