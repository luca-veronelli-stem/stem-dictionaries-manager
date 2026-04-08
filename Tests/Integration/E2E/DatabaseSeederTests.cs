using Core.Enums;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Tests.Integration.E2E;

/// <summary>
/// Test E2E per il DatabaseSeeder.
/// Verifica che il seed del dizionario standard crei tutte le variabili attese.
/// </summary>
public class DatabaseSeederTests : IntegrationTestBase
{
    [Fact]
    public async Task SeedAsync_CreatesStandardDictionary()
    {
        // Act
        await DatabaseSeeder.SeedAsync(Context);

        // Assert — dizionario standard creato
        var dict = await Context.Dictionaries
            .FirstOrDefaultAsync(d => d.IsStandard);
        Assert.NotNull(dict);
        Assert.Equal("Standard", dict.Name);
        Assert.True(dict.IsStandard);
    }

    [Fact]
    public async Task SeedAsync_StandardDictionary_Has24Variables()
    {
        // Act
        await DatabaseSeeder.SeedAsync(Context);

        // Assert
        var dict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .OrderBy(v => v.AddressLow)
            .ToListAsync();

        Assert.Equal(24, variables.Count);
    }

    [Fact]
    public async Task SeedAsync_AllStandardVariables_HaveAddressHigh00()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.All(variables, v => Assert.Equal(0x00, v.AddressHigh));
    }

    [Fact]
    public async Task SeedAsync_TipoScheda_IsEnabledAndReadOnly()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var tipoScheda = await Context.Variables
            .FirstAsync(v => v.AddressLow == 0x04);

        Assert.True(tipoScheda.IsEnabled);
        Assert.Equal(AccessMode.ReadOnly, tipoScheda.AccessMode);
        Assert.Equal(DataTypeKind.Other, tipoScheda.DataTypeKind);
    }

    [Fact]
    public async Task SeedAsync_Comandi_IsDisabledGlobally()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var comandi = await Context.Variables
            .FirstAsync(v => v.AddressLow == 0x07);

        Assert.False(comandi.IsEnabled);
        Assert.Equal(AccessMode.ReadOnly, comandi.AccessMode);
    }

    [Fact]
    public async Task SeedAsync_Stato_IsOtherType()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stato = await Context.Variables
            .FirstAsync(v => v.AddressLow == 0x05);

        Assert.Equal(DataTypeKind.Other, stato.DataTypeKind);
        Assert.Equal("3 * uint32_t", stato.DataTypeRaw);
    }

    [Fact]
    public async Task SeedAsync_Allarmi_IsBitmappedWithWordSize16()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var allarmi = await Context.Variables
            .FirstAsync(v => v.AddressLow == 0x06);

        Assert.Equal(DataTypeKind.Bitmapped, allarmi.DataTypeKind);
        Assert.Equal(2, allarmi.DataTypeParam);
        Assert.Equal(16, allarmi.WordSize);
        Assert.Equal("Bitmapped[2]", allarmi.DataTypeRaw);
    }

    [Fact]
    public async Task SeedAsync_Allarmi_NoCommonBitInterpretations()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var allarmi = await Context.Variables
            .Include(v => v.BitInterpretations)
            .FirstAsync(v => v.AddressLow == 0x06);

        // Nessuna interpretazione comune (DictionaryId=null), solo per-dizionario
        var common = allarmi.BitInterpretations.Where(bi => bi.DictionaryId == null).ToList();
        Assert.Empty(common);
    }

    [Fact]
    public async Task SeedAsync_StatoIngressiFisici_IsBitmapped32()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var ingressi = await Context.Variables
            .FirstAsync(v => v.AddressLow == 0x15);

        Assert.Equal(DataTypeKind.Bitmapped, ingressi.DataTypeKind);
        Assert.Equal(1, ingressi.DataTypeParam);
        Assert.Equal(32, ingressi.WordSize);
    }

    [Fact]
    public async Task SeedAsync_StatoUsciteFisiche_IsBitmapped32()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var uscite = await Context.Variables
            .FirstAsync(v => v.AddressLow == 0x16);

        Assert.Equal(DataTypeKind.Bitmapped, uscite.DataTypeKind);
        Assert.Equal(1, uscite.DataTypeParam);
        Assert.Equal(32, uscite.WordSize);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_DoesNotDuplicateOnSecondRun()
    {
        // Act — run twice
        await DatabaseSeeder.SeedAsync(Context);
        await DatabaseSeeder.SeedAsync(Context);

        // Assert — only one dictionary, same count
        var dictCount = await Context.Dictionaries.CountAsync(d => d.IsStandard);
        Assert.Equal(1, dictCount);
    }

    [Fact]
    public async Task SeedAsync_VariableAddresses_AreUnique()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var addresses = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .Select(v => new { v.AddressHigh, v.AddressLow })
            .ToListAsync();

        // Nessun duplicato
        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }

    // === Dizionario Pulsantiere ===

    [Fact]
    public async Task SeedAsync_CreatesPulsantiereDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstOrDefaultAsync(d => d.Name == "Pulsantiere");
        Assert.NotNull(dict);
        Assert.False(dict.IsStandard);
    }

    [Fact]
    public async Task SeedAsync_PulsantiereDictionary_Has6Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.Equal(6, variables.Count);
    }

    [Fact]
    public async Task SeedAsync_PulsantiereVariables_HaveAddressHigh80()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.All(variables, v => Assert.Equal(0x80, v.AddressHigh));
    }

    [Fact]
    public async Task SeedAsync_StatoSistema_IsDisabled()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");
        var statoSistema = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x01);

        Assert.False(statoSistema.IsEnabled);
        Assert.Equal(DataTypeKind.Bool, statoSistema.DataTypeKind);
    }

    [Fact]
    public async Task SeedAsync_ComandoLedVerde_IsBitmapped4Words8Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");
        var led = await Context.Variables
            .Include(v => v.BitInterpretations)
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x02);

        Assert.Equal(DataTypeKind.Bitmapped, led.DataTypeKind);
        Assert.Equal("Bitmapped[4]", led.DataTypeRaw);
        Assert.Equal(4, led.DataTypeParam);
        Assert.Equal(8, led.WordSize);
        Assert.Equal(AccessMode.ReadWrite, led.AccessMode);

        // 4 bit interpretations su Word 3 (attivazione) + 3 su Word 0/1/2 (timing)
        Assert.Equal(7, led.BitInterpretations.Count);

        // Word 3: attivazione LED
        var word3 = led.BitInterpretations.Where(bi => bi.WordIndex == 3).ToList();
        Assert.Equal(4, word3.Count);
        Assert.Contains(word3, bi => bi.BitIndex == 0 && bi.Meaning == "Led acceso fisso");
        Assert.Contains(word3, bi => bi.BitIndex == 1 && bi.Meaning == "Led lampeggiante");
        Assert.Contains(word3, bi => bi.BitIndex == 2);
        Assert.Contains(word3, bi => bi.BitIndex == 3);

        // Word 0/1/2: timing (bit 0 con descrizione)
        Assert.Contains(led.BitInterpretations, bi => bi.WordIndex == 0 && bi.Meaning!.Contains("pausa"));
        Assert.Contains(led.BitInterpretations, bi => bi.WordIndex == 1 && bi.Meaning!.Contains("OFF"));
        Assert.Contains(led.BitInterpretations, bi => bi.WordIndex == 2 && bi.Meaning!.Contains("ON"));
    }

    [Fact]
    public async Task SeedAsync_ComandoBuzzer_IsBitmapped4Words8Bits_2BitInterpretations()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");
        var buzzer = await Context.Variables
            .Include(v => v.BitInterpretations)
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x04);

        Assert.Equal(DataTypeKind.Bitmapped, buzzer.DataTypeKind);
        Assert.Equal(4, buzzer.DataTypeParam);
        Assert.Equal(8, buzzer.WordSize);

        // 3 bit interpretations su Word 3 (attivazione) + 3 su Word 0/1/2 (timing)
        Assert.Equal(6, buzzer.BitInterpretations.Count);

        // Word 3: attivazione buzzer
        var word3 = buzzer.BitInterpretations.Where(bi => bi.WordIndex == 3).ToList();
        Assert.Equal(3, word3.Count);
        Assert.Contains(word3, bi => bi.BitIndex == 0 && bi.Meaning!.Contains("Buzzer"));
        Assert.Contains(word3, bi => bi.BitIndex == 1 && bi.Meaning!.Contains("Buzzer"));
        Assert.Contains(word3, bi => bi.BitIndex == 2);

        // Word 0/1/2: timing (bit 0 con descrizione)
        Assert.Contains(buzzer.BitInterpretations, bi => bi.WordIndex == 0 && bi.Meaning!.Contains("pausa"));
        Assert.Contains(buzzer.BitInterpretations, bi => bi.WordIndex == 1 && bi.Meaning!.Contains("OFF"));
        Assert.Contains(buzzer.BitInterpretations, bi => bi.WordIndex == 2 && bi.Meaning!.Contains("ON"));
    }

    [Fact]
    public async Task SeedAsync_PulsantiereBoards_LinkedToDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");

        // Board pulsantiera con FW=4 o FW=15 devono puntare al dizionario
        // Sherpa Slim (FW=2) non usa il dizionario Pulsantiere
        var linkedBoards = await Context.Boards
            .Where(b => b.DictionaryId == dict.Id)
            .ToListAsync();

        // 17 board: TopLift-M(1) + Eden-XP(3) + TopLift-A2(3+1vecchia) + Optimus-XP(3) + R3L-XP(3) + Eden-BS8(3)
        Assert.Equal(17, linkedBoards.Count);
        Assert.All(linkedBoards, b => Assert.True(
            b.Name.Contains("Pulsantiera", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task SeedAsync_NonLinkedBoards_HaveNoDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        // Board senza dizionario: schede non-pulsantiera + Sherpa Slim pulsantiera (FW=2)
        var unlinkedBoards = await Context.Boards
            .Where(b => b.DictionaryId == null)
            .ToListAsync();

        Assert.True(unlinkedBoards.Count > 0);
        // Solo la pulsantiera Sherpa Slim (FW=2) può restare senza dizionario
        var pulsWithoutDict = unlinkedBoards
            .Where(b => b.Name.Contains("Pulsantiera", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Single(pulsWithoutDict);
        Assert.Equal(2, pulsWithoutDict[0].FirmwareType); // FW=2 = Sherpa Slim
    }

    [Fact]
    public async Task SeedAsync_PulsantiereBoards_Total18_AllLinked()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");

        // 18 board totali con "Pulsantiera" nel nome
        var allPulsBoards = await Context.Boards
            .Where(b => b.Name.Contains("Pulsantiera"))
            .ToListAsync();
        Assert.Equal(18, allPulsBoards.Count);

        // 17 linkate (FW=4 e FW=15), 1 non linkata (Sherpa FW=2)
        var linked = allPulsBoards.Where(b => b.DictionaryId == dict.Id).ToList();
        var unlinked = allPulsBoards.Where(b => b.DictionaryId == null).ToList();
        Assert.Equal(17, linked.Count);
        Assert.Single(unlinked);
        Assert.Equal(2, unlinked[0].FirmwareType); // FW=2 = Sherpa Slim
    }

    // === Override variabili standard per-dizionario (v7) ===

    [Fact]
    public async Task SeedAsync_PulsantiereOverrides_DisablesAllStandardExcept0x00And0x01()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var pulsDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Pulsantiere");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == pulsDict.Id)
            .ToListAsync();

        // 24 variabili standard, 2 attive (0x00, 0x01) → 22 override
        Assert.Equal(22, overrides.Count);
        Assert.All(overrides, o => Assert.False(o.IsEnabled));
    }

    [Fact]
    public async Task SeedAsync_PulsantiereOverrides_DoNotOverrideFirmwareMacchina()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var pulsDict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");

        var fwMacchina = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x00);
        var fwScheda = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x01);

        var overrideIds = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == pulsDict.Id)
            .Select(o => o.StandardVariableId)
            .ToListAsync();

        Assert.DoesNotContain(fwMacchina.Id, overrideIds);
        Assert.DoesNotContain(fwScheda.Id, overrideIds);
    }

    [Fact]
    public async Task SeedAsync_PulsantiereOverrides_IncludesModelloAndMatricola()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var pulsDict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");

        // Modello (0x02) e Matricola (0x03) devono essere disattivate
        var modello = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x02);
        var matricola = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x03);

        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == pulsDict.Id)
            .ToListAsync();

        var modelloOverride = overrides.First(o => o.StandardVariableId == modello.Id);
        var matricolaOverride = overrides.First(o => o.StandardVariableId == matricola.Id);

        Assert.False(modelloOverride.IsEnabled);
        Assert.False(matricolaOverride.IsEnabled);
    }

    // === Dizionario HMI Spyke ===

    [Fact]
    public async Task SeedAsync_CreatesDisplaySpykeDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstOrDefaultAsync(d => d.Name == "Display Spyke");
        Assert.NotNull(dict);
        Assert.False(dict.IsStandard);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpykeDictionary_Has34Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.Equal(34, variables.Count);
        Assert.All(variables, v => Assert.Equal(0x80, v.AddressHigh));
    }

    [Fact]
    public async Task SeedAsync_DisplaySpyke_BoardDisplayLinked()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var spyke = await Context.Devices
            .FirstAsync(d => d.Name == "Spyke");

        // Board "Display" (FW=8) di Spyke deve puntare al dizionario
        var displayBoard = await Context.Boards
            .FirstAsync(b => b.DeviceId == spyke.Id && b.FirmwareType == 8);

        Assert.Equal(dict.Id, displayBoard.DictionaryId);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpyke_StatoPulsanti_IsReadWrite()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var stato = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x00);

        Assert.Equal("Stato pulsanti", stato.Name);
        Assert.Equal(AccessMode.ReadWrite, stato.AccessMode);
        Assert.Equal(DataTypeKind.UInt16, stato.DataTypeKind);
        Assert.Equal(0, stato.MinValue);
        Assert.Equal(3, stato.MaxValue);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpyke_FloatVariables_AreReadOnly()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");

        // Angoli X/Y/Z (0x02-0x04) + Accelerazioni (0x05-0x07) + Touch (0x08-0x09)
        var floatVars = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x02 && v.AddressLow <= 0x09)
            .ToListAsync();

        Assert.Equal(8, floatVars.Count);
        Assert.All(floatVars, v =>
        {
            Assert.Equal(DataTypeKind.Float, v.DataTypeKind);
            Assert.Equal(AccessMode.ReadOnly, v.AccessMode);
        });
    }

    [Fact]
    public async Task SeedAsync_DisplaySpyke_OffsetAngles_AreReadWrite()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");

        // Angolo X/Y/Z offset (0x1D-0x1F)
        var offsets = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x1D && v.AddressLow <= 0x1F)
            .ToListAsync();

        Assert.Equal(3, offsets.Count);
        Assert.All(offsets, v =>
        {
            Assert.Equal(DataTypeKind.Float, v.DataTypeKind);
            Assert.Equal(AccessMode.ReadWrite, v.AccessMode);
        });
    }

    [Fact]
    public async Task SeedAsync_DisplaySpyke_FirmwareHmi_HasFormat()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var fwHmi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x17);

        Assert.Equal("Firmware HMI", fwHmi.Name);
        Assert.Equal(DataTypeKind.UInt16, fwHmi.DataTypeKind);
        Assert.Equal("255.255", fwHmi.Format);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpyke_SensoreSherpa_ReadOnlyWithDescription()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var sherpa = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x0C);

        Assert.Equal("Sensore Sherpa", sherpa.Name);
        Assert.Equal(DataTypeKind.Bool, sherpa.DataTypeKind);
        Assert.Equal(AccessMode.ReadOnly, sherpa.AccessMode);
        Assert.Equal("1 = Agganciato", sherpa.Description);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpyke_AddressesAreUnique()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var addresses = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .Select(v => new { v.AddressHigh, v.AddressLow })
            .ToListAsync();

        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }

    // === Override HMI Spyke ===

    [Fact]
    public async Task SeedAsync_DisplaySpykeOverrides_Disables3Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var displayDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == displayDict.Id && !o.IsEnabled)
            .Include(o => o.StandardVariable)
            .ToListAsync();

        // 0x08 Temperatura, 0x09 Secondi parziale, 0x0A Secondi totale
        Assert.Equal(3, overrides.Count);
        var addresses = overrides
            .Select(o => o.StandardVariable.AddressLow).OrderBy(a => a).ToList();
        Assert.Equal([0x08, 0x09, 0x0A], addresses);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpykeOverrides_CicliHaveDescriptions()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var displayDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == displayDict.Id && o.Description != null)
            .Include(o => o.StandardVariable)
            .ToListAsync();

        // 4 cicli (0x0B-0x0E) con descrizione specifica
        Assert.Equal(4, overrides.Count);

        var cicliParziale = overrides
            .First(o => o.StandardVariable.AddressLow == 0x0B);
        Assert.True(cicliParziale.IsEnabled);
        Assert.Contains("agganci al 10G resettabile", cicliParziale.Description);

        var cicliTotale = overrides
            .First(o => o.StandardVariable.AddressLow == 0x0C);
        Assert.Contains("non resettabile", cicliTotale.Description);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpykeOverrides_TotalCount7()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var displayDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == displayDict.Id)
            .ToListAsync();

        // 3 disabilitati + 4 con descrizione = 7
        Assert.Equal(7, overrides.Count);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpykeAllarmi_Word0_Has5Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var displayDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var allarmi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x06);

        var word0 = await Context.Set<BitInterpretationEntity>()
            .Where(bi => bi.VariableId == allarmi.Id
                && bi.DictionaryId == displayDict.Id
                && bi.WordIndex == 0)
            .OrderBy(bi => bi.BitIndex)
            .ToListAsync();

        Assert.Equal(5, word0.Count);
        Assert.Equal("Errore CAN", word0[0].Meaning);
        Assert.Equal("Tensione troppo bassa", word0[1].Meaning);
        Assert.Equal("Errore touch", word0[2].Meaning);
        Assert.Contains("10G", word0[3].Meaning);
        Assert.Contains("Sovraccarico celle", word0[4].Meaning);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpykeAllarmi_Word1_Has9Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var displayDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var allarmi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x06);

        var word1 = await Context.Set<BitInterpretationEntity>()
            .Where(bi => bi.VariableId == allarmi.Id
                && bi.DictionaryId == displayDict.Id
                && bi.WordIndex == 1)
            .OrderBy(bi => bi.BitIndex)
            .ToListAsync();

        Assert.Equal(9, word1.Count);
        Assert.Equal("Tensione bassa", word1[0].Meaning);
        Assert.Contains("NFC non presente", word1[1].Meaning);
        Assert.Contains("Incoerenza leva/pulsante in scarico", word1[8].Meaning);
    }

    [Fact]
    public async Task SeedAsync_DisplaySpykeAllarmi_ArePerDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var displayDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Display Spyke");
        var allarmi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x06);

        var allBits = await Context.Set<BitInterpretationEntity>()
            .Where(bi => bi.VariableId == allarmi.Id
                && bi.DictionaryId == displayDict.Id)
            .ToListAsync();

        // 5 word0 + 9 word1 = 14 total, tutti per-dizionario
        Assert.Equal(14, allBits.Count);
        Assert.All(allBits, bi => Assert.Equal(displayDict.Id, bi.DictionaryId));
    }

    // === Dizionario Gateway Spyke ===

    [Fact]
    public async Task SeedAsync_CreatesGatewaySpykeDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstOrDefaultAsync(d => d.Name == "Gateway Spyke");
        Assert.NotNull(dict);
        Assert.False(dict.IsStandard);
    }

    [Fact]
    public async Task SeedAsync_GatewaySpykeDictionary_Has9Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.Equal(9, variables.Count);
        Assert.All(variables, v => Assert.Equal(0x80, v.AddressHigh));
    }

    [Fact]
    public async Task SeedAsync_GatewaySpyke_BoardGatewayLinked()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");
        var spyke = await Context.Devices
            .FirstAsync(d => d.Name == "Spyke");

        // Board "Gateway" (FW=7) di Spyke deve puntare al dizionario
        var gatewayBoard = await Context.Boards
            .FirstAsync(b => b.DeviceId == spyke.Id && b.FirmwareType == 7);

        Assert.Equal(dict.Id, gatewayBoard.DictionaryId);
    }

    [Fact]
    public async Task SeedAsync_GatewaySpyke_CustomVariables_AreOther()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");

        // Dati SIM (0x05), Stato Gateway (0x06), Stato BLE (0x07), Stato LTE (0x08)
        var customVars = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x05 && v.AddressLow <= 0x08)
            .ToListAsync();

        Assert.Equal(4, customVars.Count);
        Assert.All(customVars, v =>
        {
            Assert.Equal(DataTypeKind.Other, v.DataTypeKind);
            Assert.Equal("Custom", v.DataTypeRaw);
        });
    }

    [Fact]
    public async Task SeedAsync_GatewaySpyke_Luci_IsReadWrite()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");
        var luci = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x04);

        Assert.Equal("Luci", luci.Name);
        Assert.Equal(DataTypeKind.UInt8, luci.DataTypeKind);
        Assert.Equal(AccessMode.ReadWrite, luci.AccessMode);
    }

    // === Override Gateway Spyke ===

    [Fact]
    public async Task SeedAsync_GatewaySpykeOverrides_Disables9Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var gwDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == gwDict.Id && !o.IsEnabled)
            .Include(o => o.StandardVariable)
            .ToListAsync();

        // 0x08-0x0F (8) + 0x17 (1) = 9
        Assert.Equal(9, overrides.Count);
        var addresses = overrides
            .Select(o => o.StandardVariable.AddressLow)
            .OrderBy(a => a).ToList();
        Assert.Equal(
            [0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x17],
            addresses);
    }

    [Fact]
    public async Task SeedAsync_GatewaySpykeAllarmi_Word0_Has5Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var gwDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");
        var allarmi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x06);

        var word0 = await Context.Set<BitInterpretationEntity>()
            .Where(bi => bi.VariableId == allarmi.Id
                && bi.DictionaryId == gwDict.Id
                && bi.WordIndex == 0)
            .OrderBy(bi => bi.BitIndex)
            .ToListAsync();

        Assert.Equal(5, word0.Count);
        Assert.Equal("Errore CAN", word0[0].Meaning);
        Assert.Equal("NFC non risponde", word0[1].Meaning);
        Assert.Equal("Mancanza SIM", word0[2].Meaning);
        Assert.Equal("Modulo IoT non risponde", word0[3].Meaning);
        Assert.Equal("Modulo BLE non risponde", word0[4].Meaning);
    }

    [Fact]
    public async Task SeedAsync_GatewaySpykeAllarmi_Word1_IsEmpty()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var gwDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");
        var allarmi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x06);

        var word1 = await Context.Set<BitInterpretationEntity>()
            .Where(bi => bi.VariableId == allarmi.Id
                && bi.DictionaryId == gwDict.Id
                && bi.WordIndex == 1)
            .ToListAsync();

        Assert.Empty(word1);
    }

    [Fact]
    public async Task SeedAsync_GatewaySpyke_AddressesAreUnique()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Gateway Spyke");
        var addresses = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .Select(v => new { v.AddressHigh, v.AddressLow })
            .ToListAsync();

        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }

    // === Dizionario Gradino ===

    [Fact]
    public async Task SeedAsync_CreatesGradinoDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstOrDefaultAsync(d => d.Name == "Azionamento Gradino");
        Assert.NotNull(dict);
        Assert.False(dict.IsStandard);
    }

    [Fact]
    public async Task SeedAsync_GradinoDictionary_Has35Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.Equal(35, variables.Count);
        Assert.All(variables, v => Assert.Equal(0x80, v.AddressHigh));
    }

    [Fact]
    public async Task SeedAsync_Gradino_BoardAzionamentoLinked()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var gradino = await Context.Devices
            .FirstAsync(d => d.Name == "Gradino");

        var board = await Context.Boards
            .FirstAsync(b => b.DeviceId == gradino.Id && b.FirmwareType == 6);

        Assert.Equal(dict.Id, board.DictionaryId);
    }

    [Fact]
    public async Task SeedAsync_Gradino_StatoKeyboard1_IsDisabled()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var var0 = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x00);

        Assert.Equal("Stato keyboard 1", var0.Name);
        Assert.False(var0.IsEnabled);
    }

    [Fact]
    public async Task SeedAsync_Gradino_Last4Variables_AreDisabled()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");

        // Motor Type (0x1F), Stato Scheda (0x20), An_Pot1 (0x21), An_Pot2 (0x22)
        var disabledVars = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x1F && v.AddressLow <= 0x22)
            .ToListAsync();

        Assert.Equal(4, disabledVars.Count);
        Assert.All(disabledVars, v => Assert.False(v.IsEnabled));
    }

    [Fact]
    public async Task SeedAsync_Gradino_CurrentVariables_HaveUnit()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");

        // I Motore (0x05), I max/media (0x11-0x18), Max current x3 (0x1C-0x1E)
        var currentVars = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id && v.Unit == "Ampere/100")
            .ToListAsync();

        Assert.Equal(12, currentVars.Count);
    }

    // === Override Gradino ===

    [Fact]
    public async Task SeedAsync_GradinoOverrides_Disables11Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var gradDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == gradDict.Id && !o.IsEnabled)
            .Include(o => o.StandardVariable)
            .ToListAsync();

        // 0x05 + 0x07-0x0F (9) + 0x17 = 11
        Assert.Equal(11, overrides.Count);
        var addresses = overrides
            .Select(o => o.StandardVariable.AddressLow)
            .OrderBy(a => a).ToList();
        Assert.Equal(
            [0x05, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x17],
            addresses);
    }

    [Fact]
    public async Task SeedAsync_GradinoOverrides_StatoHasDescription()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var gradDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var stato = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x05);

        var ov = await Context.StandardVariableOverrides
            .FirstAsync(o => o.DictionaryId == gradDict.Id
                && o.StandardVariableId == stato.Id);

        Assert.False(ov.IsEnabled);
        Assert.NotNull(ov.Description);
        Assert.Contains("OPENING_CALIBRATION", ov.Description);
        Assert.Contains("SLOWDOWN_OPENING", ov.Description);
    }

    [Fact]
    public async Task SeedAsync_GradinoIngressi_Has4Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var gradDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var ingressi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x15);

        var bits = await Context.Set<BitInterpretationEntity>()
            .Where(bi => bi.VariableId == ingressi.Id
                && bi.DictionaryId == gradDict.Id)
            .OrderBy(bi => bi.BitIndex)
            .ToListAsync();

        Assert.Equal(4, bits.Count);
        Assert.Equal("DOOR", bits[0].Meaning);
        Assert.Equal("FS OPEN", bits[1].Meaning);
        Assert.Equal("FS CLOSE", bits[2].Meaning);
        Assert.Equal("FC STEP", bits[3].Meaning);
    }

    [Fact]
    public async Task SeedAsync_GradinoUscite_Has1Bit()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var stdDict = await Context.Dictionaries.FirstAsync(d => d.IsStandard);
        var gradDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var uscite = await Context.Variables
            .FirstAsync(v => v.DictionaryId == stdDict.Id && v.AddressLow == 0x16);

        var bits = await Context.Set<BitInterpretationEntity>()
            .Where(bi => bi.VariableId == uscite.Id
                && bi.DictionaryId == gradDict.Id)
            .ToListAsync();

        Assert.Single(bits);
        Assert.Equal("LED1", bits[0].Meaning);
    }

    [Fact]
    public async Task SeedAsync_Gradino_AddressesAreUnique()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Gradino");
        var addresses = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .Select(v => new { v.AddressHigh, v.AddressLow })
            .ToListAsync();

        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }

    // === Dizionario Sherpa Slim ===

    [Fact]
    public async Task SeedAsync_CreatesSherpaSlimDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstOrDefaultAsync(d => d.Name == "Azionamento Sherpa Slim");
        Assert.NotNull(dict);
        Assert.False(dict.IsStandard);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlimDictionary_Has64Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var variables = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.Equal(64, variables.Count);
        Assert.All(variables, v => Assert.Equal(0x80, v.AddressHigh));
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_BoardAzionamentoLinked()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var sherpa = await Context.Devices
            .FirstAsync(d => d.Name == "Sherpa Slim");

        var board = await Context.Boards
            .FirstAsync(b => b.DeviceId == sherpa.Id && b.FirmwareType == 1);

        Assert.Equal(dict.Id, board.DictionaryId);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_StatoKeyboard_IsDisabled()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var var0 = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x00);

        Assert.Equal("Stato keyboard", var0.Name);
        Assert.False(var0.IsEnabled);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_MotorType_IsOtherEnum()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var motorType = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x02);

        Assert.Equal("Motor Type", motorType.Name);
        Assert.Equal(DataTypeKind.Other, motorType.DataTypeKind);
        Assert.Equal("Enum", motorType.DataTypeRaw);
        Assert.Equal(AccessMode.ReadOnly, motorType.AccessMode);
        Assert.Contains("DC BRUSHLESS", motorType.Description);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_MotorRunning_IsBoolWithDescription()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var motorRunning = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x01);

        Assert.Equal("Motor Running", motorRunning.Name);
        Assert.Equal(DataTypeKind.Bool, motorRunning.DataTypeKind);
        Assert.Equal(AccessMode.ReadOnly, motorRunning.AccessMode);
        Assert.Contains("motore fermo", motorRunning.Description);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_PidVariables_AreReadWrite()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");

        // Position PID KP (0x06), KI (0x07), Speed PID KP (0x0B), KI (0x0C),
        // Kp I PID (0x10), Ki I PID (0x11)
        byte[] rwAddresses = [0x06, 0x07, 0x0B, 0x0C, 0x10, 0x11];
        var rwVars = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && rwAddresses.Contains(v.AddressLow))
            .ToListAsync();

        Assert.Equal(6, rwVars.Count);
        Assert.All(rwVars, v => Assert.Equal(AccessMode.ReadWrite, v.AccessMode));
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_PosizioniPunti_AreInt32ReadOnly()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");

        // Posizione punto 1-12 (0x32-0x3D)
        var posizioni = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x32 && v.AddressLow <= 0x3D)
            .ToListAsync();

        Assert.Equal(12, posizioni.Count);
        Assert.All(posizioni, v =>
        {
            Assert.Equal(DataTypeKind.Int32, v.DataTypeKind);
            Assert.Equal(AccessMode.ReadOnly, v.AccessMode);
        });
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_StatoMacchina_HasDescription()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var statoMacchina = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x3E);

        Assert.Equal("Stato macchina a stati", statoMacchina.Name);
        Assert.Equal(DataTypeKind.UInt32, statoMacchina.DataTypeKind);
        Assert.Contains("macchina in idle", statoMacchina.Description);
        Assert.Contains("in errore", statoMacchina.Description);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_DatiApprendimento_AreOtherStruct()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");

        // Dati Apprendimento punto 1-13 (0x46-0x52)
        var datiApp = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x46 && v.AddressLow <= 0x52)
            .ToListAsync();

        Assert.Equal(13, datiApp.Count);
        Assert.All(datiApp, v =>
        {
            Assert.Equal(DataTypeKind.Other, v.DataTypeKind);
            Assert.Equal("Struct[32]", v.DataTypeRaw);
            Assert.Equal(AccessMode.ReadOnly, v.AccessMode);
            Assert.Contains("automata_point_t", v.Description);
        });

        // Punto 1 ha descrizione completa con campi struct
        var punto1 = datiApp.First(v => v.AddressLow == 0x46);
        Assert.Contains("isValid", punto1.Description);
        Assert.Contains("height", punto1.Description);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_DatiCorrentiFunzionamento_IsOtherStruct56()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var datiCorrente = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x53);

        Assert.Equal("Dati correnti funzionamento", datiCorrente.Name);
        Assert.Equal(DataTypeKind.Other, datiCorrente.DataTypeKind);
        Assert.Equal("Struct[56]", datiCorrente.DataTypeRaw);
        Assert.Contains("automata_handler_t", datiCorrente.Description);
        Assert.Contains("RemoteZeroPos", datiCorrente.Description);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_ThermalFault_HasUnitMs()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var thermal = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id && v.AddressLow == 0x17);

        Assert.Equal("Time for out of control thermal fault", thermal.Name);
        Assert.Equal(DataTypeKind.Int32, thermal.DataTypeKind);
        Assert.Equal("ms", thermal.Unit);
        Assert.Equal(AccessMode.ReadWrite, thermal.AccessMode);
    }

    [Fact]
    public async Task SeedAsync_SherpaSlim_AddressesAreUnique()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var addresses = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .Select(v => new { v.AddressHigh, v.AddressLow })
            .ToListAsync();

        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }

    // === Override Sherpa Slim ===

    [Fact]
    public async Task SeedAsync_SherpaSlimOverrides_Disables1Variable()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var sherpaDict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Azionamento Sherpa Slim");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == sherpaDict.Id)
            .Include(o => o.StandardVariable)
            .ToListAsync();

        // Solo 0x05 (Stato)
        Assert.Single(overrides);
        Assert.False(overrides[0].IsEnabled);
        Assert.Equal(0x05, overrides[0].StandardVariable.AddressLow);
    }

    // ====================================================================
    // Optimus-XP
    // ====================================================================

    [Fact]
    public async Task SeedAsync_CreatesOptimusXPDictionary()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstOrDefaultAsync(d => d.Name == "Madre Optimus-Xp");
        Assert.NotNull(dict);
        Assert.False(dict.IsStandard);
    }

    [Fact]
    public async Task SeedAsync_OptimusXPDictionary_Has132Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var count = await Context.Variables
            .CountAsync(v => v.DictionaryId == dict.Id);

        Assert.Equal(132, count);
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_AddressesAreUnique()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var addresses = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id)
            .Select(v => (int)v.AddressHigh << 8 | v.AddressLow)
            .ToListAsync();

        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_BoardMadreLinked()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var board = await Context.Boards
            .FirstAsync(b => b.DictionaryId == dict.Id);

        Assert.Equal(17, board.FirmwareType);
        Assert.True(board.IsPrimary);
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_SystemOn_IsBoolReadOnly()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var v = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id
                && v.AddressLow == 0x03);

        Assert.Equal("SystemOn", v.Name);
        Assert.Equal(DataTypeKind.Bool, v.DataTypeKind);
        Assert.Equal(AccessMode.ReadOnly, v.AccessMode);
        Assert.True(v.IsEnabled);
        Assert.Contains("piano spento", v.Description!);
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_StatoFinecorsa_IsBitmapped()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var v = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id
                && v.AddressLow == 0x11);

        Assert.Equal("Stato finecorsa", v.Name);
        Assert.Equal(DataTypeKind.Bitmapped, v.DataTypeKind);
        Assert.Equal(8, v.WordSize);
        Assert.True(v.IsEnabled);

        var bits = await Context.Set<BitInterpretationEntity>()
            .Where(b => b.VariableId == v.Id)
            .OrderBy(b => b.BitIndex)
            .ToListAsync();

        Assert.Equal(2, bits.Count);
        Assert.Equal("Finecorsa piano esteso", bits[0].Meaning);
        Assert.Equal("Finecorsa piano chiuso", bits[1].Meaning);
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_StatoLuci_IsBitmapped()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var v = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id
                && v.AddressLow == 0x43);

        Assert.Equal("Stato Luci", v.Name);
        Assert.Equal(DataTypeKind.Bitmapped, v.DataTypeKind);
        Assert.Equal(8, v.WordSize);
        Assert.True(v.IsEnabled);

        var bits = await Context.Set<BitInterpretationEntity>()
            .Where(b => b.VariableId == v.Id)
            .OrderBy(b => b.BitIndex)
            .ToListAsync();

        Assert.Equal(3, bits.Count);
        Assert.Equal("B", bits[0].Meaning);
        Assert.Equal("G", bits[1].Meaning);
        Assert.Equal("R", bits[2].Meaning);
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_KeyboardVars_AreDisabled()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var keyboards = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x00 && v.AddressLow <= 0x02)
            .ToListAsync();

        Assert.Equal(3, keyboards.Count);
        Assert.All(keyboards, v => Assert.False(v.IsEnabled));
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_PotenzioVars_AreReadWrite()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var potenzio = await Context.Variables
            .Where(v => v.DictionaryId == dict.Id
                && v.AddressLow >= 0x07 && v.AddressLow <= 0x0A)
            .ToListAsync();

        Assert.Equal(4, potenzio.Count);
        Assert.All(potenzio, v =>
        {
            Assert.Equal(AccessMode.ReadWrite, v.AccessMode);
            Assert.Equal("bits", v.Unit);
            Assert.True(v.IsEnabled);
        });
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_SoglieVoltage_AreFloat()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");

        var undervoltage = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id
                && v.AddressLow == 0x57);
        Assert.Equal(DataTypeKind.Float, undervoltage.DataTypeKind);
        Assert.Equal("volts", undervoltage.Unit);
        Assert.True(undervoltage.IsEnabled);

        var carica = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id
                && v.AddressLow == 0x58);
        Assert.Equal(DataTypeKind.Float, carica.DataTypeKind);
        Assert.Equal("volts", carica.Unit);
        Assert.True(carica.IsEnabled);
    }

    [Fact]
    public async Task SeedAsync_OptimusXP_TipoBarella_HasDescription()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var v = await Context.Variables
            .FirstAsync(v => v.DictionaryId == dict.Id
                && v.AddressLow == 0x80);

        Assert.Equal("Tipo di barella", v.Name);
        Assert.Contains("Stryker Powerload", v.Description!);
        Assert.Contains("Kartsana Superbravo", v.Description!);
        Assert.True(v.IsEnabled);
    }

    [Fact]
    public async Task SeedAsync_OptimusXPOverrides_Disables2Variables()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var overrides = await Context.StandardVariableOverrides
            .Where(o => o.DictionaryId == dict.Id)
            .Include(o => o.StandardVariable)
            .ToListAsync();

        // 0x05 (Stato) e 0x08 (Temperatura scheda)
        Assert.Equal(2, overrides.Count);
        Assert.All(overrides, o => Assert.False(o.IsEnabled));
        var addresses = overrides
            .Select(o => o.StandardVariable.AddressLow)
            .OrderBy(x => x).ToArray();
        Assert.Equal(new byte[] { 0x05, 0x08 }, addresses);
    }

    [Fact]
    public async Task SeedAsync_OptimusXPAllarmi_Has9Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var std = await Context.Dictionaries
            .FirstAsync(d => d.IsStandard);
        var allarmi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == std.Id
                && v.AddressLow == 0x06);

        var bits = await Context.Set<BitInterpretationEntity>()
            .Where(b => b.VariableId == allarmi.Id
                && b.DictionaryId == dict.Id)
            .ToListAsync();

        // 6 bit in Word 0 + 3 bit in Word 1
        Assert.Equal(9, bits.Count);
        Assert.Contains(bits, b =>
            b.WordIndex == 0 && b.Meaning == "Sovracorrente pompa");
        Assert.Contains(bits, b =>
            b.WordIndex == 1 && b.Meaning == "Low battery");
    }

    [Fact]
    public async Task SeedAsync_OptimusXPIngressi_Has6Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var std = await Context.Dictionaries
            .FirstAsync(d => d.IsStandard);
        var ingressi = await Context.Variables
            .FirstAsync(v => v.DictionaryId == std.Id
                && v.AddressLow == 0x15);

        var bits = await Context.Set<BitInterpretationEntity>()
            .Where(b => b.VariableId == ingressi.Id
                && b.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.Equal(6, bits.Count);
        Assert.Contains(bits, b =>
            b.BitIndex == 0 && b.Meaning == "Ingresso barella");
        Assert.Contains(bits, b =>
            b.BitIndex == 11 && b.Meaning == "Comando tastiera stop");
    }

    [Fact]
    public async Task SeedAsync_OptimusXPUscite_Has6Bits()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries
            .FirstAsync(d => d.Name == "Madre Optimus-Xp");
        var std = await Context.Dictionaries
            .FirstAsync(d => d.IsStandard);
        var uscite = await Context.Variables
            .FirstAsync(v => v.DictionaryId == std.Id
                && v.AddressLow == 0x16);

        var bits = await Context.Set<BitInterpretationEntity>()
            .Where(b => b.VariableId == uscite.Id
                && b.DictionaryId == dict.Id)
            .ToListAsync();

        Assert.Equal(6, bits.Count);
        Assert.Contains(bits, b =>
            b.BitIndex == 0 && b.Meaning == "EVA");
        Assert.Contains(bits, b =>
            b.BitIndex == 11 && b.Meaning == "LEDR");
    }
}
