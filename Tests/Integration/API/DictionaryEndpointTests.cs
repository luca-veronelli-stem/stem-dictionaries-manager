using API.Dtos;
using API.Mapping;
using Core.Enums;

namespace Tests.Integration.API;

/// <summary>
/// Integration tests per Dictionary endpoints.
/// Copre: lista, standard standalone, dettaglio, variabili risolte (BR-API-002).
/// </summary>
public class DictionaryEndpointTests : ApiIntegrationTestBase
{
    // === GET /api/dictionaries ===

    [Fact]
    public async Task GetDictionaries_ReturnsAll()
    {
        await SeedFullScenarioAsync();

        var dicts = await DictionaryService.GetAllAsync();

        Assert.Equal(2, dicts.Count);
    }

    [Fact]
    public async Task GetDictionaries_VariableCountIsEnabledOnly()
    {
        var (_, _, stdDictId, specDictId) = await SeedFullScenarioAsync();

        // Standard: 3 variabili (2 abilitate, 1 disabilitata)
        var stdVars = await VariableService.GetByDictionaryIdAsync(stdDictId);
        var stdEnabled = stdVars.Count(v => v.IsEnabled);
        Assert.Equal(2, stdEnabled);

        // Specifico: 2 variabili (1 abilitata, 1 disabilitata)
        var specVars = await VariableService.GetByDictionaryIdAsync(specDictId);
        var specEnabled = specVars.Count(v => v.IsEnabled);
        Assert.Equal(1, specEnabled);
    }

    // === GET /api/dictionaries/standard ===

    [Fact]
    public async Task GetStandard_ReturnsOnlyEnabled()
    {
        await SeedFullScenarioAsync();

        var standard = await DictionaryService.GetStandardDictionaryAsync();
        Assert.NotNull(standard);

        var vars = await VariableService.GetByDictionaryIdAsync(standard!.Id);
        var enabledVars = vars.Where(v => v.IsEnabled)
            .OrderBy(v => v.FullAddress)
            .Select(ApiMapper.ToVariableDto)
            .ToList();

        // 3 variabili standard, ma 1 disabilitata
        Assert.Equal(2, enabledVars.Count);
        Assert.DoesNotContain(enabledVars, v => v.Name == "Deprecata");
    }

    [Fact]
    public async Task GetStandard_WhenNotExists_ReturnsNull()
    {
        var standard = await DictionaryService.GetStandardDictionaryAsync();

        Assert.Null(standard);
    }

    // === GET /api/dictionaries/{id} ===

    [Fact]
    public async Task GetDictionary_ReturnsOnlyEnabledVariables()
    {
        var (_, _, _, specDictId) = await SeedFullScenarioAsync();

        var dict = await DictionaryService.GetByIdAsync(specDictId);
        Assert.NotNull(dict);

        var vars = await VariableService.GetByDictionaryIdAsync(specDictId);
        var enabledVars = vars.Where(v => v.IsEnabled)
            .Select(ApiMapper.ToVariableDto).ToList();

        Assert.Single(enabledVars);
        Assert.Equal("SystemOn", enabledVars[0].Name);
    }

    [Fact]
    public async Task GetDictionary_NotFound_ReturnsNull()
    {
        var dict = await DictionaryService.GetByIdAsync(999);

        Assert.Null(dict);
    }

    // === GET /api/dictionaries/{id}/resolved ===

    [Fact]
    public async Task GetResolved_IncludesStandardAndSpecific()
    {
        var (_, _, stdDictId, specDictId) = await SeedFullScenarioAsync();

        var stdDict = await DictionaryService.GetStandardDictionaryAsync();
        Assert.NotNull(stdDict);

        // Simula logica dell'endpoint resolved
        var resolved = new List<ResolvedVariableDto>();

        // Standard risolte
        var stdVars = await VariableService.GetByDictionaryIdAsync(stdDict!.Id);
        var overrides = await VariableService.GetOverridesByDictionaryAsync(specDictId);

        foreach (var sv in stdVars)
        {
            var ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
            // BR-009: deprecata globalmente → skip
            if (!sv.IsEnabled) continue;
            // BR-009: override → usa override
            var enabled = ov?.IsEnabled ?? sv.IsEnabled;
            if (!enabled) continue;

            resolved.Add(ApiMapper.ToResolvedDto(sv, isStandard: true,
                overrideDescription: ov?.Description));
        }

        // Specifiche abilitate
        var specVars = await VariableService.GetByDictionaryIdAsync(specDictId);
        foreach (var v in specVars.Where(v => v.IsEnabled))
            resolved.Add(ApiMapper.ToResolvedDto(v, isStandard: false));

        resolved = [.. resolved.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow)];

        // Firmware (abilitata, no override) + SystemOn (specifica)
        // Matricola: override isEnabled=false → esclusa
        // Deprecata: isEnabled=false globalmente → esclusa
        // Disabilitata: specifica isEnabled=false → esclusa
        Assert.Equal(2, resolved.Count);
        Assert.Equal("Firmware", resolved[0].Name);
        Assert.True(resolved[0].IsStandard);
        Assert.Equal("SystemOn", resolved[1].Name);
        Assert.False(resolved[1].IsStandard);
    }

    [Fact]
    public async Task GetResolved_OverrideDescription_AppliedCorrectly()
    {
        var (_, _, _, specDictId) = await SeedFullScenarioAsync();

        // Matricola ha override "Non usato su Optimus" + isEnabled=false
        var overrides = await VariableService.GetOverridesByDictionaryAsync(specDictId);
        var matricolaOverride = overrides.FirstOrDefault(o => o.Description != null);

        Assert.NotNull(matricolaOverride);
        Assert.Equal("Non usato su Optimus", matricolaOverride!.Description);
        Assert.False(matricolaOverride.IsEnabled);
    }

    [Fact]
    public async Task GetResolved_OrderedByFullAddress()
    {
        var (_, _, _, specDictId) = await SeedFullScenarioAsync();

        var stdDict = await DictionaryService.GetStandardDictionaryAsync();
        var stdVars = await VariableService.GetByDictionaryIdAsync(stdDict!.Id);
        var overrides = await VariableService.GetOverridesByDictionaryAsync(specDictId);
        var specVars = await VariableService.GetByDictionaryIdAsync(specDictId);

        var resolved = new List<ResolvedVariableDto>();
        foreach (var sv in stdVars.Where(v => v.IsEnabled))
        {
            var ov = overrides.FirstOrDefault(o => o.StandardVariableId == sv.Id);
            if (!(ov?.IsEnabled ?? sv.IsEnabled)) continue;
            resolved.Add(ApiMapper.ToResolvedDto(sv, isStandard: true));
        }
        foreach (var v in specVars.Where(v => v.IsEnabled))
            resolved.Add(ApiMapper.ToResolvedDto(v, isStandard: false));

        resolved = [.. resolved.OrderBy(v => (v.AddressHigh << 8) | v.AddressLow)];

        // 0x0000 (Firmware) < 0x8003 (SystemOn)
        Assert.True(resolved.Count >= 2);
        var first = (resolved[0].AddressHigh << 8) | resolved[0].AddressLow;
        var second = (resolved[1].AddressHigh << 8) | resolved[1].AddressLow;
        Assert.True(first < second);
    }
}
