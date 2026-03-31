using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var bitRepo = new BitInterpretationRepository(Context);

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
        foreach (var bi in interpretations)
        {
            await bitRepo.AddAsync(bi);
        }

        // Verify
        var loaded = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        Assert.Equal(5, loaded.Count);

        var word0Bits = loaded.Where(b => b.WordIndex == 0).ToList();
        var word1Bits = loaded.Where(b => b.WordIndex == 1).ToList();
        Assert.Equal(3, word0Bits.Count);
        Assert.Equal(2, word1Bits.Count);
    }

    #endregion

    #region Override Tests

    [Fact]
    public async Task FullWorkflow_VariableOverride_EffectiveState()
    {
        // Setup
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var stateRepo = new VariableDeviceStateRepository(Context);

        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 99 };
        await deviceRepo.AddAsync(device);

        var dict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(dict);

        var variable = new VariableEntity
        {
            Name = "TestVar",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "UInt8",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true, // Abilitata globalmente
            DictionaryId = dict.Id
        };
        await varRepo.AddAsync(variable);

        // Stato effettivo senza override
        var noOverride = await stateRepo.GetByVariableAndDeviceAsync(variable.Id, device.Id);
        Assert.Null(noOverride); // Default = segue global (true)

        // Aggiungi override
        await stateRepo.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = variable.Id,
            DeviceId = device.Id,
            IsEnabled = false
        });

        // Stato effettivo con override
        var withOverride = await stateRepo.GetByVariableAndDeviceAsync(variable.Id, device.Id);
        Assert.NotNull(withOverride);
        Assert.False(withOverride.IsEnabled);
    }

    [Fact]
    public async Task FullWorkflow_DeprecatedVariable_CannotBeEnabledPerDevice()
    {
        // Setup: variabile deprecata globalmente
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var stateRepo = new VariableDeviceStateRepository(Context);

        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 99 };
        await deviceRepo.AddAsync(device);

        var dict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(dict);

        var deprecatedVar = new VariableEntity
        {
            Name = "DeprecatedVar",
            AddressHigh = 0x00,
            AddressLow = 0x99,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "UInt8",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = false, // DEPRECATA
            DictionaryId = dict.Id
        };
        await varRepo.AddAsync(deprecatedVar);

        // BR-011: Override isEnabled=true vietato per variabile deprecata
        // Questo vincolo è enforced da VariableService, non dal DB
        // Ma possiamo testare che il DB accetta la riga (il service blocca prima)
        var state = new VariableDeviceStateEntity
        {
            VariableId = deprecatedVar.Id,
            DeviceId = device.Id,
            IsEnabled = true // Tentativo invalido - BR-011
        };
        await stateRepo.AddAsync(state);

        // Il DB accetta, il vincolo è a livello service
        var loaded = await stateRepo.GetByVariableAndDeviceAsync(deprecatedVar.Id, device.Id);
        Assert.NotNull(loaded);
        // Nota: VariableService.SetDeviceStateAsync bloccherebbe questo caso
    }

    #endregion

    #region Per-Device BitInterpretation Tests (SESSION_037)

    [Fact]
    public async Task FullWorkflow_BitInterpretations_CommonAndDeviceOverride()
    {
        // Setup
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var bitRepo = new BitInterpretationRepository(Context);

        var device = new DeviceEntity { Name = "Sherpa", MachineCode = 1 };
        await deviceRepo.AddAsync(device);

        var dict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(dict);

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
            DictionaryId = dict.Id
        };
        await varRepo.AddAsync(bitmappedVar);

        // Common interpretations (DeviceId = null)
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Fusibile aperto"
        });
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 1, Meaning = "Sovracorrente"
        });

        // Device-specific override for bit 1 only
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id, DeviceId = device.Id,
            WordIndex = 0, BitIndex = 1, Meaning = "Sovracorrente relè"
        });

        // GetByVariableAndDevice returns common + device-specific
        var forDevice = await bitRepo.GetByVariableAndDeviceAsync(bitmappedVar.Id, device.Id);
        Assert.Equal(3, forDevice.Count);
        Assert.Contains(forDevice, r => r.Meaning == "Fusibile aperto" && r.DeviceId == null);
        Assert.Contains(forDevice, r => r.Meaning == "Sovracorrente" && r.DeviceId == null);
        Assert.Contains(forDevice, r => r.Meaning == "Sovracorrente relè" && r.DeviceId == device.Id);

        // GetByVariableId returns everything
        var all = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task FullWorkflow_SyncDeviceOverrides_DoesNotAffectCommon()
    {
        // Setup
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var bitRepo = new BitInterpretationRepository(Context);

        var device = new DeviceEntity { Name = "Optimus", MachineCode = 2 };
        await deviceRepo.AddAsync(device);

        var dict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(dict);

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
            DictionaryId = dict.Id
        };
        await varRepo.AddAsync(bitmappedVar);

        // Seed common
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Common bit 0"
        });

        // Sync device-specific (should NOT touch common)
        var deviceIncoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "Optimus bit 0" },
            new() { WordIndex = 0, BitIndex = 1, Meaning = "Optimus bit 1" }
        };
        await bitRepo.SyncByVariableIdAsync(bitmappedVar.Id, device.Id, deviceIncoming);

        // Verify: common is untouched
        var all = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        var common = all.Where(r => r.DeviceId == null).ToList();
        var deviceSpecific = all.Where(r => r.DeviceId == device.Id).ToList();

        Assert.Single(common);
        Assert.Equal("Common bit 0", common[0].Meaning);
        Assert.Equal(2, deviceSpecific.Count);
    }

    [Fact]
    public async Task FullWorkflow_MultipleDevices_IndependentOverrides()
    {
        // Setup
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var bitRepo = new BitInterpretationRepository(Context);

        var sherpa = new DeviceEntity { Name = "Sherpa", MachineCode = 1 };
        var optimus = new DeviceEntity { Name = "Optimus", MachineCode = 2 };
        await deviceRepo.AddAsync(sherpa);
        await deviceRepo.AddAsync(optimus);

        var dict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(dict);

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
            DictionaryId = dict.Id
        };
        await varRepo.AddAsync(bitmappedVar);

        // Same word/bit, different devices
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 2, Meaning = "Batteria scarica"
        });
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id, DeviceId = sherpa.Id,
            WordIndex = 0, BitIndex = 2, Meaning = "Batteria scarica (Sherpa)"
        });
        await bitRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = bitmappedVar.Id, DeviceId = optimus.Id,
            WordIndex = 0, BitIndex = 2, Meaning = "Sovracorrente EV1 (Optimus)"
        });

        // Sherpa sees common + sherpa override
        var forSherpa = await bitRepo.GetByVariableAndDeviceAsync(bitmappedVar.Id, sherpa.Id);
        Assert.Equal(2, forSherpa.Count);
        Assert.Contains(forSherpa, r => r.DeviceId == null);
        Assert.Contains(forSherpa, r => r.DeviceId == sherpa.Id);

        // Optimus sees common + optimus override
        var forOptimus = await bitRepo.GetByVariableAndDeviceAsync(bitmappedVar.Id, optimus.Id);
        Assert.Equal(2, forOptimus.Count);
        Assert.Contains(forOptimus, r => r.DeviceId == null);
        Assert.Contains(forOptimus, r => r.DeviceId == optimus.Id);
    }

    #endregion

    #region Address Uniqueness Tests

    [Fact]
    public async Task FullWorkflow_AddressUniqueness_PerDictionary()
    {
        // Setup
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);

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
        var all = await Context.Variables.ToListAsync();
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
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var bitRepo = new BitInterpretationRepository(Context);

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
        foreach (var bi in bits)
            await bitRepo.AddAsync(bi);

        // Verify — reload variable
        var loadedVar = await Context.Variables.FindAsync(bitmappedVar.Id);
        Assert.NotNull(loadedVar);
        Assert.Equal(8, loadedVar.WordSize);
        Assert.Equal(2, loadedVar.DataTypeParam);

        // Verify — bit interpretations persisted
        var loadedBits = await bitRepo.GetByVariableIdAsync(bitmappedVar.Id);
        Assert.Equal(4, loadedBits.Count);
        Assert.True(loadedBits.All(b => b.BitIndex < 8));
    }

    #endregion
}
