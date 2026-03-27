using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Integration;

namespace Tests.Integration.E2E;

/// <summary>
/// Test E2E per il workflow completo dei dispositivi.
/// Verifica flussi CRUD Device, Board, stato comandi/variabili per device.
/// </summary>
public class DeviceWorkflowTests : IntegrationTestBase
{
    /// <summary>
    /// Calcola ProtocolAddress come fa il domain model Board.
    /// </summary>
    private static uint CalculateProtocolAddress(int machineCode, int fwType, int boardNum)
        => (uint)((machineCode << 16) | ((fwType & 0x03FF) << 6) | (boardNum & 0x003F));

    #region Device CRUD Tests

    [Fact]
    public async Task FullWorkflow_CreateDevice_AddBoards_AssignDictionaries()
    {
        // 1. Crea device
        var deviceRepo = new DeviceRepository(Context);
        var device = new DeviceEntity
        {
            Name = "Eden-XP",
            MachineCode = 3,
            Description = "Supporto barella ammortizzato"
        };
        await deviceRepo.AddAsync(device);

        // 2. Crea dizionari
        var dictRepo = new DictionaryRepository(Context);
        var mainDict = new DictionaryEntity { Name = "Eden-XP Main", IsStandard = false };
        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(mainDict);
        await dictRepo.AddAsync(stdDict);

        // 3. Crea board con dizionari (ProtocolAddress calcolato)
        var boardRepo = new BoardRepository(Context);
        var motherboard = new BoardEntity
        {
            Name = "Madre",
            DeviceId = device.Id,
            FirmwareType = 17,
            BoardNumber = 1,
            IsPrimary = true,
            DictionaryId = mainDict.Id,
            ProtocolAddress = CalculateProtocolAddress(device.MachineCode, 17, 1)
        };
        var peripheral = new BoardEntity
        {
            Name = "Pulsantiera",
            DeviceId = device.Id,
            FirmwareType = 4,
            BoardNumber = 2,
            IsPrimary = false,
            DictionaryId = null,
            ProtocolAddress = CalculateProtocolAddress(device.MachineCode, 4, 2)
        };
        await boardRepo.AddAsync(motherboard);
        await boardRepo.AddAsync(peripheral);

        // 4. Verifica
        var boards = await boardRepo.GetByDeviceIdAsync(device.Id);
        Assert.Equal(2, boards.Count);

        var primary = boards.FirstOrDefault(b => b.IsPrimary);
        Assert.NotNull(primary);
        Assert.Equal("Madre", primary.Name);
        Assert.Equal(mainDict.Id, primary.DictionaryId);
    }

    [Fact]
    public async Task FullWorkflow_DeviceDetail_ShowsCorrectDictionaries()
    {
        // Setup: Device con board che puntano a dizionari diversi
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var boardRepo = new BoardRepository(Context);

        var device = new DeviceEntity { Name = "R3L-XP", MachineCode = 11 };
        await deviceRepo.AddAsync(device);

        var masterDict = new DictionaryEntity { Name = "R3L-XP Master", IsStandard = false };
        var slaveDict = new DictionaryEntity { Name = "R3L-XP Slave", IsStandard = false };
        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(masterDict);
        await dictRepo.AddAsync(slaveDict);
        await dictRepo.AddAsync(stdDict);

        await boardRepo.AddAsync(new BoardEntity
        {
            Name = "Master",
            DeviceId = device.Id,
            FirmwareType = 11,
            BoardNumber = 1,
            IsPrimary = true,
            DictionaryId = masterDict.Id,
            ProtocolAddress = CalculateProtocolAddress(device.MachineCode, 11, 1)
        });
        await boardRepo.AddAsync(new BoardEntity
        {
            Name = "Slave",
            DeviceId = device.Id,
            FirmwareType = 12,
            BoardNumber = 2,
            IsPrimary = false,
            DictionaryId = slaveDict.Id,
            ProtocolAddress = CalculateProtocolAddress(device.MachineCode, 12, 2)
        });

        // Query: dizionari visibili per questo device
        var deviceBoards = await Context.Boards
            .Where(b => b.DeviceId == device.Id)
            .ToListAsync();
        var linkedDictIds = deviceBoards
            .Where(b => b.DictionaryId.HasValue)
            .Select(b => b.DictionaryId!.Value)
            .Distinct()
            .ToList();
        var standardDict = await dictRepo.GetStandardDictionaryAsync();

        // Verify: deve vedere Master, Slave, Standard
        Assert.Equal(2, linkedDictIds.Count);
        Assert.NotNull(standardDict);
        // Total: 3 dizionari visibili
    }

    #endregion

    #region Device State Tests

    [Fact]
    public async Task FullWorkflow_DeviceCommands_OverridesArePerDevice()
    {
        // Setup: 2 device, 1 comando, override diversi
        var deviceRepo = new DeviceRepository(Context);
        var cmdRepo = new CommandRepository(Context);
        var stateRepo = new CommandDeviceStateRepository(Context);

        var device1 = new DeviceEntity { Name = "Eden-XP", MachineCode = 3 };
        var device2 = new DeviceEntity { Name = "Spark", MachineCode = 7 };
        await deviceRepo.AddAsync(device1);
        await deviceRepo.AddAsync(device2);

        var command = new CommandEntity
        {
            Name = "ReadVariable",
            CodeHigh = 0x00,
            CodeLow = 0x01,
            IsResponse = false
        };
        await cmdRepo.AddAsync(command);

        // Override: disabilitato per Eden-XP, abilitato per Spark
        await stateRepo.AddAsync(new CommandDeviceStateEntity
        {
            CommandId = command.Id,
            DeviceId = device1.Id,
            IsEnabled = false
        });
        await stateRepo.AddAsync(new CommandDeviceStateEntity
        {
            CommandId = command.Id,
            DeviceId = device2.Id,
            IsEnabled = true
        });

        // Verify
        var state1 = await stateRepo.GetByCommandAndDeviceAsync(command.Id, device1.Id);
        var state2 = await stateRepo.GetByCommandAndDeviceAsync(command.Id, device2.Id);

        Assert.NotNull(state1);
        Assert.False(state1.IsEnabled);

        Assert.NotNull(state2);
        Assert.True(state2.IsEnabled);
    }

    [Fact]
    public async Task FullWorkflow_DeviceVariables_OverridesArePerDevice()
    {
        // Setup: 2 device, 1 variabile standard, override diversi
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var stateRepo = new VariableDeviceStateRepository(Context);

        var device1 = new DeviceEntity { Name = "Eden-XP", MachineCode = 3 };
        var device2 = new DeviceEntity { Name = "Spark", MachineCode = 7 };
        await deviceRepo.AddAsync(device1);
        await deviceRepo.AddAsync(device2);

        var stdDict = new DictionaryEntity { Name = "Standard", IsStandard = true };
        await dictRepo.AddAsync(stdDict);

        var variable = new VariableEntity
        {
            Name = "FirmwareVersion",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "UInt16",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            DictionaryId = stdDict.Id
        };
        await varRepo.AddAsync(variable);

        // Override: disabilitato per Eden-XP, default (null) per Spark
        await stateRepo.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = variable.Id,
            DeviceId = device1.Id,
            IsEnabled = false
        });

        // Verify
        var state1 = await stateRepo.GetByVariableAndDeviceAsync(variable.Id, device1.Id);
        var state2 = await stateRepo.GetByVariableAndDeviceAsync(variable.Id, device2.Id);

        Assert.NotNull(state1);
        Assert.False(state1.IsEnabled);

        Assert.Null(state2); // Nessun override = segue default
    }

    [Fact]
    public async Task FullWorkflow_DeleteDevice_CascadesToBoards()
    {
        // Setup
        var deviceRepo = new DeviceRepository(Context);
        var boardRepo = new BoardRepository(Context);

        var device = new DeviceEntity { Name = "ToDelete", MachineCode = 99 };
        await deviceRepo.AddAsync(device);

        await boardRepo.AddAsync(new BoardEntity
        {
            Name = "Board1",
            DeviceId = device.Id,
            FirmwareType = 1,
            BoardNumber = 1,
            ProtocolAddress = CalculateProtocolAddress(device.MachineCode, 1, 1)
        });
        await boardRepo.AddAsync(new BoardEntity
        {
            Name = "Board2",
            DeviceId = device.Id,
            FirmwareType = 2,
            BoardNumber = 2,
            ProtocolAddress = CalculateProtocolAddress(device.MachineCode, 2, 2)
        });

        // Act
        await deviceRepo.DeleteAsync(device.Id);

        // Verify
        var boards = await Context.Boards.Where(b => b.DeviceId == device.Id).ToListAsync();
        Assert.Empty(boards);
    }

    #endregion
}
