using Core.Enums;
using Infrastructure;
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

        // Board senza dizionario: non-pulsantiera + Sherpa Pulsantiera (FW=2, non nel CSV)
        var unlinkedBoards = await Context.Boards
            .Where(b => b.DictionaryId == null)
            .ToListAsync();

        Assert.True(unlinkedBoards.Count > 0);
        // Sherpa Pulsantiera (FW=2) non è nel CSV pulsantiere, resta senza dizionario
        var sherpaPuls = unlinkedBoards.FirstOrDefault(b =>
            b.Name.Contains("Pulsantiera", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(sherpaPuls);
    }

    [Fact]
    public async Task SeedAsync_PulsantiereBoards_Total18_Linked17()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var dict = await Context.Dictionaries.FirstAsync(d => d.Name == "Pulsantiere");

        // 18 board totali con "Pulsantiera" nel nome
        var allPulsBoards = await Context.Boards
            .Where(b => b.Name.Contains("Pulsantiera"))
            .ToListAsync();
        Assert.Equal(18, allPulsBoards.Count);

        // 17 linkate (FW=4 o FW=15), 1 non linkata (Sherpa FW=2)
        var linked = allPulsBoards.Where(b => b.DictionaryId == dict.Id).ToList();
        var unlinked = allPulsBoards.Where(b => b.DictionaryId == null).ToList();
        Assert.Equal(17, linked.Count);
        Assert.Single(unlinked); // Sherpa Pulsantiera (FW=2)
    }

    // === Override per-dizionario (v7) ===
    // TODO: Aggiungere test quando DatabaseSeeder implementa StandardVariableOverride seeding
}
