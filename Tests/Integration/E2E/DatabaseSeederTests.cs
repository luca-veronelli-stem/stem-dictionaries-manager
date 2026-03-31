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
    public async Task SeedAsync_Allarmi_NoBitInterpretations()
    {
        await DatabaseSeeder.SeedAsync(Context);

        var allarmi = await Context.Variables
            .Include(v => v.BitInterpretations)
            .FirstAsync(v => v.AddressLow == 0x06);

        Assert.Empty(allarmi.BitInterpretations);
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
}
