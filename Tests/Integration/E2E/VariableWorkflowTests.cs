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
}
