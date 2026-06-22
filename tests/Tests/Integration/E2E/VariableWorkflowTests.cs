using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Integration;

namespace Tests.Integration.E2E;

/// <summary>
/// Test E2E per il workflow completo delle variabili.
/// Verifica bitmapped, override, unicità indirizzi.
/// </summary>
public class VariableWorkflowTests : IntegrationTestBase
{
    #region Bitmapped Tests

    [Fact]
    public async Task FullWorkflow_CreateBitmappedVariable_WithInterpretations()
    {
        // Setup: Dictionary + Variable bitmapped
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var bitRepo = new BitInterpretationRepository(Context, NullLogger<RepositoryBase<BitInterpretationEntity>>.Instance);

        var dict = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        await dictRepo.AddAsync(dict);

        var bitmappedVar = new VariableEntity
        {
            Name = "StatusFlags",
            AddressHigh = 0x80,
            AddressLow = 0x10,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "Bitmapped[2]",
            DataTypeParam = 2, // 2 word
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            DictionaryId = dict.Id
        };
        await varRepo.AddAsync(bitmappedVar);

        // Add interpretations per 2 word
        var interpretations = new List<BitInterpretationEntity>
        {
            new() { VariableId = bitmappedVar.Id, WordIndex = 0, BitIndex = 0, Meaning = "Power On" },
            new() { VariableId = bitmappedVar.Id, WordIndex = 0, BitIndex = 1, Meaning = "Error" },
            new() { VariableId = bitmappedVar.Id, WordIndex = 0, BitIndex = 2, Meaning = "Warning" },
            new() { VariableId = bitmappedVar.Id, WordIndex = 1, BitIndex = 0, Meaning = "Motor Running" },
            new() { VariableId = bitmappedVar.Id, WordIndex = 1, BitIndex = 1, Meaning = "Brake Active" },
        };
        foreach (BitInterpretationEntity bi in interpretations)
        {
            await bitRepo.AddAsync(bi);
        }

        // Verify
        IReadOnlyList<BitInterpretationEntity> loaded = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        Assert.Equal(5, loaded.Count);

        var word0Bits = loaded.Where(b => b.WordIndex == 0).ToList();
        var word1Bits = loaded.Where(b => b.WordIndex == 1).ToList();
        Assert.Equal(3, word0Bits.Count);
        Assert.Equal(2, word1Bits.Count);
    }

    #endregion

    #region Override Tests (v7 — StandardVariableOverride)

    [Fact]
    public async Task FullWorkflow_VariableOverride_EffectiveState()
    {
        // Setup
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var overrideRepo = new StandardVariableOverrideRepository(Context, NullLogger<RepositoryBase<StandardVariableOverrideEntity>>.Instance);

        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(stdDict);

        var nonStdDict = new DictionaryEntity { Name = "Eden-XP", IsStandard = false };
        await dictRepo.AddAsync(nonStdDict);

        var variable = new VariableEntity
        {
            Name = "TestVar",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "UInt8",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true, // Abilitata globalmente
            DictionaryId = stdDict.Id
        };
        await varRepo.AddAsync(variable);

        // Stato effettivo senza override
        StandardVariableOverrideEntity? noOverride = await overrideRepo.GetByDictionaryAndVariableAsync(nonStdDict.Id, variable.Id);
        Assert.Null(noOverride); // Default = segue global (true)

        // Aggiungi override
        await overrideRepo.AddAsync(new StandardVariableOverrideEntity
        {
            StandardVariableId = variable.Id,
            DictionaryId = nonStdDict.Id,
            IsEnabled = false
        });

        // Stato effettivo con override
        StandardVariableOverrideEntity? withOverride = await overrideRepo.GetByDictionaryAndVariableAsync(nonStdDict.Id, variable.Id);
        Assert.NotNull(withOverride);
        Assert.False(withOverride.IsEnabled);
    }

    [Fact]
    public async Task FullWorkflow_DeprecatedVariable_OverrideDbAcceptsButServiceBlocks()
    {
        // Setup: variabile deprecata globalmente
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var overrideRepo = new StandardVariableOverrideRepository(Context, NullLogger<RepositoryBase<StandardVariableOverrideEntity>>.Instance);

        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(stdDict);

        var nonStdDict = new DictionaryEntity { Name = "Eden-XP", IsStandard = false };
        await dictRepo.AddAsync(nonStdDict);

        var deprecatedVar = new VariableEntity
        {
            Name = "DeprecatedVar",
            AddressHigh = 0x00,
            AddressLow = 0x99,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "UInt8",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = false, // DEPRECATA
            DictionaryId = stdDict.Id
        };
        await varRepo.AddAsync(deprecatedVar);

        // BR-011: Override isEnabled=true vietato per variabile deprecata
        // Questo vincolo è enforced da VariableService, non dal DB
        // Ma possiamo testare che il DB accetta la riga (il service blocca prima)
        var overrideEntity = new StandardVariableOverrideEntity
        {
            StandardVariableId = deprecatedVar.Id,
            DictionaryId = nonStdDict.Id,
            IsEnabled = true // Tentativo invalido - BR-011
        };
        await overrideRepo.AddAsync(overrideEntity);

        // Il DB accetta, il vincolo è a livello service
        StandardVariableOverrideEntity? loaded = await overrideRepo.GetByDictionaryAndVariableAsync(nonStdDict.Id, deprecatedVar.Id);
        Assert.NotNull(loaded);
        // Nota: VariableService.SetOverrideAsync bloccherebbe questo caso
    }

    #endregion

    #region Per-Dictionary BitInterpretation Tests (v7)

    [Fact]
    public async Task FullWorkflow_BitInterpretations_CommonAndDictionaryOverride()
    {
        // Setup
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var bitRepo = new BitInterpretationRepository(Context, NullLogger<RepositoryBase<BitInterpretationEntity>>.Instance);

        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(stdDict);

        var nonStdDict = new DictionaryEntity { Name = "Eden-XP", IsStandard = false };
        await dictRepo.AddAsync(nonStdDict);

        var bitmappedVar = new VariableEntity
        {
            Name = "Allarmi",
            AddressHigh = 0x00,
            AddressLow = 0x06,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[2]",
            DataTypeParam = 2,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            DictionaryId = stdDict.Id
        };
        await varRepo.AddAsync(bitmappedVar);

        // Common interpretations (DictionaryId = null)
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id,
            DictionaryId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Fusibile aperto"
        });
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id,
            DictionaryId = null,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Sovracorrente"
        });

        // Dictionary-specific override for bit 1 only
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id,
            DictionaryId = nonStdDict.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Sovracorrente relè"
        });

        // GetByVariableAndDictionary returns common + dictionary-specific
        IReadOnlyList<BitInterpretationEntity> forDict = await bitRepo.GetByVariableAndDictionaryAsync(bitmappedVar.Id, nonStdDict.Id);
        Assert.Equal(3, forDict.Count);
        Assert.Contains(forDict, r => r.Meaning == "Fusibile aperto" && r.DictionaryId == null);
        Assert.Contains(forDict, r => r.Meaning == "Sovracorrente" && r.DictionaryId == null);
        Assert.Contains(forDict, r => r.Meaning == "Sovracorrente relè" && r.DictionaryId == nonStdDict.Id);

        // GetByVariableId returns everything
        IReadOnlyList<BitInterpretationEntity> all = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task FullWorkflow_SyncDictionaryOverrides_DoesNotAffectCommon()
    {
        // Setup
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var bitRepo = new BitInterpretationRepository(Context, NullLogger<RepositoryBase<BitInterpretationEntity>>.Instance);

        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(stdDict);

        var nonStdDict = new DictionaryEntity { Name = "Eden-XP", IsStandard = false };
        await dictRepo.AddAsync(nonStdDict);

        var bitmappedVar = new VariableEntity
        {
            Name = "Allarmi",
            AddressHigh = 0x00,
            AddressLow = 0x06,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[2]",
            DataTypeParam = 2,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            DictionaryId = stdDict.Id
        };
        await varRepo.AddAsync(bitmappedVar);

        // Seed common
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id,
            DictionaryId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit 0"
        });

        // Sync dictionary-specific (should NOT touch common)
        var dictIncoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "Eden bit 0" },
            new() { WordIndex = 0, BitIndex = 1, Meaning = "Eden bit 1" }
        };
        await bitRepo.SyncByVariableIdAsync(bitmappedVar.Id, nonStdDict.Id, dictIncoming);

        // Verify: common is untouched
        IReadOnlyList<BitInterpretationEntity> all = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        var common = all.Where(r => r.DictionaryId == null).ToList();
        var dictSpecific = all.Where(r => r.DictionaryId == nonStdDict.Id).ToList();

        Assert.Single(common);
        Assert.Equal("Common bit 0", common[0].Meaning);
        Assert.Equal(2, dictSpecific.Count);
    }

    [Fact]
    public async Task FullWorkflow_MultipleDictionaries_IndependentOverrides()
    {
        // Setup
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var bitRepo = new BitInterpretationRepository(Context, NullLogger<RepositoryBase<BitInterpretationEntity>>.Instance);

        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(stdDict);

        var edenDict = new DictionaryEntity { Name = "Eden-XP", IsStandard = false };
        var sparkDict = new DictionaryEntity { Name = "Spark HMI", IsStandard = false };
        await dictRepo.AddAsync(edenDict);
        await dictRepo.AddAsync(sparkDict);

        var bitmappedVar = new VariableEntity
        {
            Name = "Allarmi",
            AddressHigh = 0x00,
            AddressLow = 0x06,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[2]",
            DataTypeParam = 2,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            DictionaryId = stdDict.Id
        };
        await varRepo.AddAsync(bitmappedVar);

        // Same word/bit, different dictionaries
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id,
            DictionaryId = null,
            WordIndex = 0,
            BitIndex = 2,
            Meaning = "Batteria scarica"
        });
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id,
            DictionaryId = edenDict.Id,
            WordIndex = 0,
            BitIndex = 2,
            Meaning = "Batteria scarica (Eden)"
        });
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id,
            DictionaryId = sparkDict.Id,
            WordIndex = 0,
            BitIndex = 2,
            Meaning = "Sovracorrente EV1 (Spark)"
        });

        // Eden sees common + eden override
        IReadOnlyList<BitInterpretationEntity> forEden = await bitRepo.GetByVariableAndDictionaryAsync(bitmappedVar.Id, edenDict.Id);
        Assert.Equal(2, forEden.Count);
        Assert.Contains(forEden, r => r.DictionaryId == null);
        Assert.Contains(forEden, r => r.DictionaryId == edenDict.Id);

        // Spark sees common + spark override
        IReadOnlyList<BitInterpretationEntity> forSpark = await bitRepo.GetByVariableAndDictionaryAsync(bitmappedVar.Id, sparkDict.Id);
        Assert.Equal(2, forSpark.Count);
        Assert.Contains(forSpark, r => r.DictionaryId == null);
        Assert.Contains(forSpark, r => r.DictionaryId == sparkDict.Id);
    }

    #endregion

    #region Address Uniqueness Tests

    [Fact]
    public async Task FullWorkflow_AddressUniqueness_PerDictionary()
    {
        // Setup
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);

        var dict1 = new DictionaryEntity { Name = "Dict1", IsStandard = false };
        var dict2 = new DictionaryEntity { Name = "Dict2", IsStandard = false };
        await dictRepo.AddAsync(dict1);
        await dictRepo.AddAsync(dict2);

        // Stesso indirizzo in dizionari diversi = OK
        await varRepo.AddAsync(new VariableEntity
        {
            Name = "Var1",
            AddressHigh = 0x80,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "UInt8",
            AccessMode = AccessMode.ReadOnly,
            DictionaryId = dict1.Id
        });
        await varRepo.AddAsync(new VariableEntity
        {
            Name = "Var2",
            AddressHigh = 0x80,
            AddressLow = 0x01, // Stesso indirizzo!
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "UInt16",
            AccessMode = AccessMode.ReadOnly,
            DictionaryId = dict2.Id // Ma dizionario diverso
        });

        // Verify - entrambe esistono
        List<VariableEntity> all = await Context.Variables.ToListAsync();
        Assert.Equal(2, all.Count);

        // Stesso indirizzo nello stesso dizionario = Errore DB
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await varRepo.AddAsync(new VariableEntity
            {
                Name = "Var3",
                AddressHigh = 0x80,
                AddressLow = 0x01, // Duplicato!
                DataTypeKind = DataTypeKind.UInt8,
                DataTypeRaw = "UInt8",
                AccessMode = AccessMode.ReadOnly,
                DictionaryId = dict1.Id // Stesso dizionario di Var1
            });
        });
    }

    #endregion

    #region WordSize Persistence Tests

    [Fact]
    public async Task FullWorkflow_CreateBitmappedWithWordSize_PersistsAndLoads()
    {
        // Setup
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var varRepo = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var bitRepo = new BitInterpretationRepository(Context, NullLogger<RepositoryBase<BitInterpretationEntity>>.Instance);

        var dict = new DictionaryEntity { Name = "WordSizeDict", IsStandard = false };
        await dictRepo.AddAsync(dict);

        // Create bitmapped variable with wordSize=8
        var bitmappedVar = new VariableEntity
        {
            Name = "CompactFlags",
            AddressHigh = 0x80,
            AddressLow = 0x30,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "Bitmapped[2]",
            DataTypeParam = 2,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            DictionaryId = dict.Id,
            WordSize = 8
        };
        await varRepo.AddAsync(bitmappedVar);

        // Add bit interpretations within 8-bit range
        var bits = new List<BitInterpretationEntity>
        {
            new() { VariableId = bitmappedVar.Id, WordIndex = 0, BitIndex = 0, Meaning = "Power" },
            new() { VariableId = bitmappedVar.Id, WordIndex = 0, BitIndex = 7, Meaning = "Error" },
            new() { VariableId = bitmappedVar.Id, WordIndex = 1, BitIndex = 0, Meaning = "Motor" },
            new() { VariableId = bitmappedVar.Id, WordIndex = 1, BitIndex = 5, Meaning = "Brake" },
        };
        foreach (BitInterpretationEntity bi in bits)
        {
            await bitRepo.AddAsync(bi);
        }

        // Verify — reload variable
        VariableEntity? loadedVar = await Context.Variables.FindAsync(bitmappedVar.Id);
        Assert.NotNull(loadedVar);
        Assert.Equal(8, loadedVar.WordSize);
        Assert.Equal(2, loadedVar.DataTypeParam);

        // Verify — bit interpretations persisted
        IReadOnlyList<BitInterpretationEntity> loadedBits = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        Assert.Equal(4, loadedBits.Count);
        Assert.True(loadedBits.All(b => b.BitIndex < 8));
    }

    #endregion
}
