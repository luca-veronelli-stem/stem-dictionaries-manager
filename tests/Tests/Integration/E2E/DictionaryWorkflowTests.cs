using Core.Models;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Integration;

namespace Tests.Integration.E2E;

/// <summary>
/// Test E2E per il workflow completo dei dizionari.
/// Usa DB SQLite in-memory per testare il full stack.
/// </summary>
public class DictionaryWorkflowTests : IntegrationTestBase
{
    /// <summary>
    /// Calcola ProtocolAddress come fa il domain model Board.
    /// </summary>
    private static uint CalculateProtocolAddress(int machineCode, int fwType, int boardNum)
        => (uint)((machineCode << 16) | ((fwType & 0x03FF) << 6) | (boardNum & 0x003F));

    #region Full Workflow Tests

    [Fact]
    public async Task FullWorkflow_CreateDictionary_AddVariables_Delete()
    {
        // 1. Crea dizionario
        var dictRepo = new DictionaryRepository(Context);
        var dictEntity = new DictionaryEntity
        {
            Name = "TestDict",
            Description = "Test dictionary",
            IsStandard = false
        };
        await dictRepo.AddAsync(dictEntity);

        // 2. Aggiungi variabili
        var varRepo = new VariableRepository(Context);
        var var1 = new VariableEntity
        {
            Name = "Var1",
            AddressHigh = 0x80,
            AddressLow = 0x01,
            DataTypeKind = Core.Enums.DataTypeKind.UInt8,
            DataTypeRaw = "UInt8",
            AccessMode = Core.Enums.AccessMode.ReadOnly,
            DictionaryId = dictEntity.Id
        };
        var var2 = new VariableEntity
        {
            Name = "Var2",
            AddressHigh = 0x80,
            AddressLow = 0x02,
            DataTypeKind = Core.Enums.DataTypeKind.UInt16,
            DataTypeRaw = "UInt16",
            AccessMode = Core.Enums.AccessMode.ReadWrite,
            DictionaryId = dictEntity.Id
        };
        await varRepo.AddAsync(var1);
        await varRepo.AddAsync(var2);

        // 3. Verifica dizionario con variabili
        DictionaryEntity? loaded = await Context.Dictionaries
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == dictEntity.Id);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Variables.Count);

        // 4. Elimina dizionario (cascade delete)
        await dictRepo.DeleteAsync(dictEntity.Id);

        // 5. Verifica cascade
        Assert.Empty(await Context.Variables.ToListAsync());
        Assert.Null(await Context.Dictionaries.FindAsync(dictEntity.Id));
    }

    [Fact]
    public async Task FullWorkflow_StandardDictionary_IsUniqueInSystem()
    {
        // 1. Crea primo dizionario standard
        var dictRepo = new DictionaryRepository(Context);
        var standard1 = new DictionaryEntity
        {
            Name = "Standard",
            IsStandard = true
        };
        await dictRepo.AddAsync(standard1);

        // 2. Verifica che esista
        DictionaryEntity? stdDict = await dictRepo.GetStandardDictionaryAsync();
        Assert.NotNull(stdDict);
        Assert.Equal("Standard", stdDict.Name);

        // 3. Tenta di creare secondo standard → il DB blocca (BR-004 partial unique index)
        var standard2 = new DictionaryEntity
        {
            Name = "AltroStandard",
            IsStandard = true
        };
        await Assert.ThrowsAsync<DbUpdateException>(() => dictRepo.AddAsync(standard2));
    }

    #endregion

    #region Semantic Tests

    [Fact]
    public async Task FullWorkflow_DictionarySemantic_Dedicated_WhenSingleDevice()
    {
        // Setup: Device, Dictionary, Board
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var boardRepo = new BoardRepository(Context);

        var device = new DeviceEntity { Name = "Eden-XP", MachineCode = 3 };
        await deviceRepo.AddAsync(device);

        var dict = new DictionaryEntity { Name = "Eden-XP Dict", IsStandard = false };
        await dictRepo.AddAsync(dict);

        var board = new BoardEntity
        {
            Name = "Madre",
            DeviceId = device.Id,
            FirmwareType = 17,
            BoardNumber = 1,
            IsPrimary = true,
            DictionaryId = dict.Id,
            ProtocolAddress = CalculateProtocolAddress(device.MachineCode, 17, 1)
        };
        await boardRepo.AddAsync(board);

        // Verify: dizionario usato solo da un device = Dedicated
        List<BoardEntity> linkedBoards = await Context.Boards
            .Where(b => b.DictionaryId == dict.Id)
            .ToListAsync();
        var deviceIds = linkedBoards.Select(b => b.DeviceId).Distinct().ToList();

        Assert.Single(deviceIds);
        // Semantic = Dedicated
    }

    [Fact]
    public async Task FullWorkflow_DictionarySemantic_Shared_WhenMultipleDevices()
    {
        // Setup: 2 Device, 1 Dictionary condiviso
        var deviceRepo = new DeviceRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var boardRepo = new BoardRepository(Context);

        var device1 = new DeviceEntity { Name = "Eden-XP", MachineCode = 3 };
        var device2 = new DeviceEntity { Name = "Spark", MachineCode = 7 };
        await deviceRepo.AddAsync(device1);
        await deviceRepo.AddAsync(device2);

        var sharedDict = new DictionaryEntity { Name = "Pulsantiere", IsStandard = false };
        await dictRepo.AddAsync(sharedDict);

        var board1 = new BoardEntity
        {
            Name = "Pulsantiera 1",
            DeviceId = device1.Id,
            FirmwareType = 4,
            BoardNumber = 2,
            DictionaryId = sharedDict.Id,
            ProtocolAddress = CalculateProtocolAddress(device1.MachineCode, 4, 2)
        };
        var board2 = new BoardEntity
        {
            Name = "Pulsantiera 2",
            DeviceId = device2.Id,
            FirmwareType = 4,
            BoardNumber = 2,
            DictionaryId = sharedDict.Id,
            ProtocolAddress = CalculateProtocolAddress(device2.MachineCode, 4, 2)
        };
        await boardRepo.AddAsync(board1);
        await boardRepo.AddAsync(board2);

        // Verify: dizionario usato da 2 device = Shared
        List<BoardEntity> linkedBoards = await Context.Boards
            .Where(b => b.DictionaryId == sharedDict.Id)
            .ToListAsync();
        var deviceIds = linkedBoards.Select(b => b.DeviceId).Distinct().ToList();

        Assert.Equal(2, deviceIds.Count);
        // Semantic = Shared
    }

    [Fact]
    public async Task FullWorkflow_DictionarySemantic_Orphan_WhenNoBoards()
    {
        // Setup: Dictionary senza board
        var dictRepo = new DictionaryRepository(Context);

        var orphanDict = new DictionaryEntity { Name = "OrphanDict", IsStandard = false };
        await dictRepo.AddAsync(orphanDict);

        // Verify: nessun board referenzia il dizionario = Orphan
        List<BoardEntity> linkedBoards = await Context.Boards
            .Where(b => b.DictionaryId == orphanDict.Id)
            .ToListAsync();

        Assert.Empty(linkedBoards);
        // Semantic = Orphan
    }

    #endregion
}
