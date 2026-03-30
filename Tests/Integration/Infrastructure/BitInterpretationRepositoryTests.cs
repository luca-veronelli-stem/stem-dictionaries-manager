using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per BitInterpretationRepository.
/// </summary>
public class BitInterpretationRepositoryTests : IntegrationTestBase
{
    private readonly BitInterpretationRepository _repository;
    private readonly VariableRepository _variableRepo;
    private readonly DictionaryRepository _dictionaryRepo;
    private VariableEntity _testVariable = null!;

    public BitInterpretationRepositoryTests()
    {
        _repository = new BitInterpretationRepository(Context);
        _variableRepo = new VariableRepository(Context);
        _dictionaryRepo = new DictionaryRepository(Context);
    }

    public override async Task InitializeAsync()
    {
        // Setup: crea dizionario e variabile bitmapped
        var dictionary = new DictionaryEntity { Name = "test-dict" };
        await _dictionaryRepo.AddAsync(dictionary);

        _testVariable = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "StatusBits",
            AddressHigh = 0x00,
            AddressLow = 0x10,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[2]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(_testVariable);
    }

    [Fact]
    public async Task AddAsync_CreatesInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Motor Running"
        };

        var result = await _repository.AddAsync(interpretation);

        Assert.True(result.Id > 0);
        Assert.Equal("Motor Running", result.Meaning);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Error Flag"
        };
        await _repository.AddAsync(interpretation);

        var result = await _repository.GetByIdAsync(interpretation.Id);

        Assert.NotNull(result);
        Assert.Equal("Error Flag", result.Meaning);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByVariableIdAsync_ReturnsInterpretations()
    {
        // Arrange - aggiungi multiple interpretazioni
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Bit 0"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Bit 1"
        });

        // Act
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByVariableIdAsync_NoResults_ReturnsEmptyList()
    {
        var result = await _repository.GetByVariableIdAsync(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByVariableIdAsync_OrdersByBitIndex()
    {
        // Arrange - aggiungi in ordine inverso
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 5,
            Meaning = "Bit 5"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 2,
            Meaning = "Bit 2"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 8,
            Meaning = "Bit 8"
        });

        // Act
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert - deve essere ordinato per BitIndex
        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].BitIndex);
        Assert.Equal(5, result[1].BitIndex);
        Assert.Equal(8, result[2].BitIndex);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 3,
            Meaning = "Original"
        };
        await _repository.AddAsync(interpretation);

        interpretation.Meaning = "Updated Meaning";
        await _repository.UpdateAsync(interpretation);

        var result = await _repository.GetByIdAsync(interpretation.Id);
        Assert.Equal("Updated Meaning", result!.Meaning);
    }

    [Fact]
    public async Task DeleteAsync_RemovesInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 1,
            BitIndex = 0,
            Meaning = "To Delete"
        };
        await _repository.AddAsync(interpretation);

        await _repository.DeleteAsync(interpretation.Id);

        var result = await _repository.GetByIdAsync(interpretation.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }

    // === SyncByVariableIdAsync Tests ===

    [Fact]
    public async Task SyncByVariableIdAsync_AddsNewAndDeletesMissing()
    {
        // Arrange - aggiungi interpretazione iniziale
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "To Delete"
        });

        // Incoming: solo bit 1 (nuovo), bit 0 non presente → deve essere eliminato
        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 1, Meaning = "New Bit" }
        };

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_UpdatesExistingMeaning()
    {
        // Arrange
        var existing = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Old Meaning"
        };
        await _repository.AddAsync(existing);
        var existingId = existing.Id;

        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "Updated Meaning" }
        };

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_PreservesIdForUnchangedRows()
    {
        // Arrange
        var existing = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 3,
            Meaning = "Unchanged"
        };
        await _repository.AddAsync(existing);
        var existingId = existing.Id;

        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 3, Meaning = "Unchanged" }
        };

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_EmptyIncoming_DeletesAll()
    {
        // Arrange
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Will Delete"
        });

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, []);
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert
        Assert.Empty(result);
    }

    // === DeviceId Tests (SESSION_037) ===

    [Fact]
    public async Task AddAsync_WithDeviceId_PersistsDeviceId()
    {
        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 50 };
        Context.Devices.Add(device);
        await Context.SaveChangesAsync();

        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DeviceId = device.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Device-specific bit"
        };

        var result = await _repository.AddAsync(interpretation);

        Assert.Equal(device.Id, result.DeviceId);
    }

    [Fact]
    public async Task AddAsync_WithNullDeviceId_PersistsNull()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DeviceId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit"
        };

        var result = await _repository.AddAsync(interpretation);

        Assert.Null(result.DeviceId);
    }

    [Fact]
    public async Task GetByVariableAndDeviceAsync_ReturnsBothCommonAndDeviceSpecific()
    {
        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 50 };
        Context.Devices.Add(device);
        await Context.SaveChangesAsync();

        // Common interpretation (DeviceId = null)
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DeviceId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit 0"
        });

        // Device-specific interpretation
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DeviceId = device.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Device bit 1"
        });

        // Another device's interpretation (should NOT appear)
        var otherDevice = new DeviceEntity { Name = "OtherDevice", MachineCode = 51 };
        Context.Devices.Add(otherDevice);
        await Context.SaveChangesAsync();

        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DeviceId = otherDevice.Id,
            WordIndex = 0,
            BitIndex = 2,
            Meaning = "Other device bit"
        });

        var result = await _repository.GetByVariableAndDeviceAsync(_testVariable.Id, device.Id);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Meaning == "Common bit 0" && r.DeviceId == null);
        Assert.Contains(result, r => r.Meaning == "Device bit 1" && r.DeviceId == device.Id);
    }

    [Fact]
    public async Task GetByVariableAndDeviceAsync_NoDeviceOverrides_ReturnsOnlyCommon()
    {
        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 50 };
        Context.Devices.Add(device);
        await Context.SaveChangesAsync();

        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DeviceId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common only"
        });

        var result = await _repository.GetByVariableAndDeviceAsync(_testVariable.Id, device.Id);

        Assert.Single(result);
        Assert.Null(result[0].DeviceId);
    }

    [Fact]
    public async Task GetByVariableIdAsync_ReturnsAllDevices()
    {
        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 50 };
        Context.Devices.Add(device);
        await Context.SaveChangesAsync();

        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = device.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Device override"
        });

        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_WithDeviceId_OnlySyncsForThatDevice()
    {
        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 50 };
        Context.Devices.Add(device);
        await Context.SaveChangesAsync();

        // Pre-existing: common + device-specific
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = device.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Old device"
        });

        // Sync only device-specific: replace with new interpretation
        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "New device" },
            new() { WordIndex = 0, BitIndex = 1, Meaning = "New device bit 1" }
        };

        await _repository.SyncByVariableIdAsync(_testVariable.Id, device.Id, incoming);

        // Common should be untouched
        var all = await _repository.GetByVariableIdAsync(_testVariable.Id);
        var common = all.Where(r => r.DeviceId == null).ToList();
        var deviceSpecific = all.Where(r => r.DeviceId == device.Id).ToList();

        Assert.Single(common);
        Assert.Equal("Common", common[0].Meaning);
        Assert.Equal(2, deviceSpecific.Count);
        Assert.Contains(deviceSpecific, r => r.Meaning == "New device");
        Assert.Contains(deviceSpecific, r => r.Meaning == "New device bit 1");
    }

    [Fact]
    public async Task SyncByVariableIdAsync_NullDeviceId_OnlySyncsCommon()
    {
        var device = new DeviceEntity { Name = "TestDevice", MachineCode = 50 };
        Context.Devices.Add(device);
        await Context.SaveChangesAsync();

        // Pre-existing: common + device-specific
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Old common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = device.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Device specific"
        });

        // Sync common: replace
        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "New common" }
        };

        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);

        // Device-specific should be untouched
        var all = await _repository.GetByVariableIdAsync(_testVariable.Id);
        var common = all.Where(r => r.DeviceId == null).ToList();
        var deviceSpecific = all.Where(r => r.DeviceId == device.Id).ToList();

        Assert.Single(common);
        Assert.Equal("New common", common[0].Meaning);
        Assert.Single(deviceSpecific);
        Assert.Equal("Device specific", deviceSpecific[0].Meaning);
    }

    [Fact]
    public async Task SameVariableSameBit_DifferentDevices_BothPersist()
    {
        var device1 = new DeviceEntity { Name = "Device1", MachineCode = 50 };
        var device2 = new DeviceEntity { Name = "Device2", MachineCode = 51 };
        Context.Devices.AddRange(device1, device2);
        await Context.SaveChangesAsync();

        // Common, Device1, Device2 — all word0 bit0 but different DeviceId
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = device1.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Device1 override"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DeviceId = device2.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Device2 override"
        });

        var all = await _repository.GetByVariableIdAsync(_testVariable.Id);

        Assert.Equal(3, all.Count);
        Assert.Contains(all, r => r.Meaning == "Common" && r.DeviceId == null);
        Assert.Contains(all, r => r.Meaning == "Device1 override" && r.DeviceId == device1.Id);
        Assert.Contains(all, r => r.Meaning == "Device2 override" && r.DeviceId == device2.Id);
    }
}
