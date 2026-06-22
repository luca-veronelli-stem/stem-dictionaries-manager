using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Services;
using Services.Interfaces;
using Tests.Integration;

namespace Tests.Integration.E2E;

/// <summary>
/// Test E2E per il workflow Audit Trail.
/// Verifica che le operazioni CRUD attraverso i service generino
/// effettivamente AuditEntry nel database.
/// </summary>
public class AuditTrailTests : IntegrationTestBase
{
    private readonly AuditEntryRepository _auditRepo;
    private readonly ICurrentUserProvider _userProvider;
    private readonly IAuditService _auditService;

    public AuditTrailTests()
    {
        SeedTestUser();
        _auditRepo = new AuditEntryRepository(Context, NullLogger<RepositoryBase<AuditEntryEntity>>.Instance);
        _userProvider = new CurrentUserProvider { CurrentUserId = 1 };
        _auditService = new AuditService(_auditRepo, NullLogger<AuditService>.Instance);
    }

    // === Dictionary ===

    [Fact]
    public async Task DictionaryAdd_CreatesAuditEntry()
    {
        DictionaryService service = CreateDictionaryService();

        Dictionary result = await service.AddAsync(
            new Dictionary("AuditTest", "desc"));

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Dictionary
                     && a.EntityId == result.Id)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Equal(1, entries[0].ChangedById);
        Assert.NotNull(entries[0].NewValue);
        Assert.Contains("AuditTest", entries[0].NewValue!);
        Assert.Null(entries[0].PreviousValue);
    }

    [Fact]
    public async Task DictionaryUpdate_CreatesAuditEntryWithPreviousAndNew()
    {
        DictionaryService service = CreateDictionaryService();
        Dictionary created = await service.AddAsync(
            new Dictionary("BeforeUpdate", "old desc"));

        var updated = Dictionary.Restore(
            created.Id, "AfterUpdate", "new desc", created.IsStandard, []);
        await service.UpdateAsync(updated);

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Dictionary
                     && a.Operation == AuditOperation.Update)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Contains("BeforeUpdate", entries[0].PreviousValue!);
        Assert.Contains("AfterUpdate", entries[0].NewValue!);
    }

    [Fact]
    public async Task DictionaryDelete_CreatesAuditEntryWithPreviousValue()
    {
        DictionaryService service = CreateDictionaryService();
        Dictionary created = await service.AddAsync(
            new Dictionary("ToDelete", "will be deleted"));

        await service.DeleteAsync(created.Id);

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Dictionary
                     && a.Operation == AuditOperation.Delete)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Contains("ToDelete", entries[0].PreviousValue!);
        Assert.Null(entries[0].NewValue);
    }

    // === Variable ===

    [Fact]
    public async Task VariableAdd_CreatesAuditEntry()
    {
        DictionaryService dictService = CreateDictionaryService();
        Dictionary dict = await dictService.AddAsync(new Dictionary("VarDict"));

        var variable = new Variable(
            "TestVar", 0x80, 0x01, DataTypeKind.UInt16,
            AccessMode.ReadWrite, "UInt16");

        Variable result = await dictService.AddVariableAsync(dict.Id, variable);

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Variable
                     && a.EntityId == result.Id)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Contains("TestVar", entries[0].NewValue!);
    }

    [Fact]
    public async Task VariableUpdate_CreatesAuditEntryWithPreviousAndNew()
    {
        VariableService varService = CreateVariableService();
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        DictionaryEntity dict = await dictRepo.AddAsync(
            new DictionaryEntity { Name = "VarUpdateDict" });

        var variable = new Variable(
            "OrigName", 0x80, 0x01, DataTypeKind.UInt16,
            AccessMode.ReadWrite, "UInt16");
        Variable created = await varService.AddAsync(dict.Id, variable);

        var updated = Variable.Restore(
            created.Id, "NewName", 0x80, 0x01,
            DataTypeKind.UInt16, "UInt16", null,
            AccessMode.ReadWrite, true, null, null, null, null, null, null);
        await varService.UpdateAsync(updated);

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Variable
                     && a.Operation == AuditOperation.Update)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Contains("OrigName", entries[0].PreviousValue!);
        Assert.Contains("NewName", entries[0].NewValue!);
    }

    // === Command ===

    [Fact]
    public async Task CommandAdd_CreatesAuditEntry()
    {
        CommandService service = CreateCommandService();

        Command result = await service.AddAsync(
            new Command("READ_VAR", 0x00, 0x01));

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Command
                     && a.EntityId == result.Id)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Contains("READ_VAR", entries[0].NewValue!);
    }

    [Fact]
    public async Task CommandDelete_CreatesAuditEntry()
    {
        CommandService service = CreateCommandService();
        Command created = await service.AddAsync(
            new Command("TO_DEL", 0x00, 0x02));

        await service.DeleteAsync(created.Id);

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Command
                     && a.Operation == AuditOperation.Delete)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Contains("TO_DEL", entries[0].PreviousValue!);
    }

    // === Board ===

    [Fact]
    public async Task BoardAdd_CreatesAuditEntry()
    {
        await SeedTestDevicesAsync();
        BoardService service = CreateBoardService();

        // DeviceId=9 → Optimus-XP, MachineCode=10
        Board result = await service.AddAsync(
            new Board(9, "TestBoard", 17, 1, machineCode: 10));

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Board
                     && a.EntityId == result.Id)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Contains("TestBoard", entries[0].NewValue!);
    }

    // === Device ===

    [Fact]
    public async Task DeviceAdd_CreatesAuditEntry()
    {
        DeviceService service = CreateDeviceService();

        Device result = await service.AddAsync(
            new Device("AuditDevice", 99, "test device"));

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Device
                     && a.EntityId == result.Id)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Contains("AuditDevice", entries[0].NewValue!);
    }

    // === Full Workflow ===

    [Fact]
    public async Task FullWorkflow_CreateUpdateDelete_TracksAllChanges()
    {
        DictionaryService service = CreateDictionaryService();

        // 1. Create
        Dictionary dict = await service.AddAsync(
            new Dictionary("Workflow", "v1"));

        // 2. Update
        var updated = Dictionary.Restore(dict.Id, "Workflow", "v2", false, []);
        await service.UpdateAsync(updated);

        // 3. Delete
        await service.DeleteAsync(dict.Id);

        // Verifica: 3 entry (Create, Update, Delete)
        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Dictionary
                     && a.EntityId == dict.Id)
            .OrderBy(a => a.ChangedAt)
            .ToListAsync();

        Assert.Equal(3, entries.Count);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Equal(AuditOperation.Update, entries[1].Operation);
        Assert.Equal(AuditOperation.Delete, entries[2].Operation);

        // Ogni entry ha il changedById corretto
        Assert.All(entries, e => Assert.Equal(1, e.ChangedById));
    }

    [Fact]
    public async Task FullWorkflow_MultipleEntities_IndependentAuditTrails()
    {
        DictionaryService dictService = CreateDictionaryService();
        CommandService cmdService = CreateCommandService();

        Dictionary dict = await dictService.AddAsync(
            new Dictionary("Multi1"));
        Command cmd = await cmdService.AddAsync(
            new Command("MULTI_CMD", 0x00, 0x10));

        List<AuditEntryEntity> dictEntries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Dictionary)
            .ToListAsync();
        List<AuditEntryEntity> cmdEntries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.Command)
            .ToListAsync();

        Assert.Single(dictEntries);
        Assert.Single(cmdEntries);
        Assert.Contains("Multi1", dictEntries[0].NewValue!);
        Assert.Contains("MULTI_CMD", cmdEntries[0].NewValue!);
    }

    // === StandardVariableOverride ===

    [Fact]
    public async Task SetOverride_CreatesAuditEntry()
    {
        VariableService varService = CreateVariableService();
        var dictRepo = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);

        // Crea dizionario standard + variabile standard
        DictionaryEntity stdDict = await dictRepo.AddAsync(
            new DictionaryEntity
            { Name = "Standard", IsStandard = true });
        var stdVar = new Variable(
            "StdVar", 0x00, 0x01, DataTypeKind.UInt16,
            AccessMode.ReadWrite, "UInt16");
        Variable createdVar = await varService.AddAsync(stdDict.Id, stdVar);

        // Crea dizionario non-standard
        DictionaryEntity myDict = await dictRepo.AddAsync(
            new DictionaryEntity
            { Name = "MyDict", IsStandard = false });

        // Setta override
        await varService.SetOverrideAsync(
            myDict.Id, createdVar.Id, true, "override desc");

        List<AuditEntryEntity> entries = await Context.AuditEntries
            .Where(a => a.EntityType == AuditEntityType.StandardVariableOverride)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Equal(AuditOperation.Create, entries[0].Operation);
        Assert.Contains("override desc", entries[0].NewValue!);
    }

    // === Helpers ===

    private DictionaryService CreateDictionaryService() =>
        new(new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance),
            new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance),
            _auditService, _userProvider, NullLogger<DictionaryService>.Instance);

    private VariableService CreateVariableService() =>
        new(new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance),
            new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance),
            new BitInterpretationRepository(Context, NullLogger<RepositoryBase<BitInterpretationEntity>>.Instance),
            new StandardVariableOverrideRepository(Context, NullLogger<RepositoryBase<StandardVariableOverrideEntity>>.Instance),
            _auditService, _userProvider, NullLogger<VariableService>.Instance);

    private CommandService CreateCommandService() =>
        new(new CommandRepository(Context, NullLogger<RepositoryBase<CommandEntity>>.Instance),
            new CommandDeviceStateRepository(Context, NullLogger<RepositoryBase<CommandDeviceStateEntity>>.Instance),
            _auditService, _userProvider, NullLogger<CommandService>.Instance);

    private BoardService CreateBoardService() =>
        new(new BoardRepository(Context, NullLogger<RepositoryBase<BoardEntity>>.Instance),
            new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance),
            _auditService, _userProvider, NullLogger<BoardService>.Instance);

    private DeviceService CreateDeviceService() =>
        new(new DeviceRepository(Context, NullLogger<RepositoryBase<DeviceEntity>>.Instance),
            new BoardRepository(Context, NullLogger<RepositoryBase<BoardEntity>>.Instance),
            new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance),
            _auditService, _userProvider, NullLogger<DeviceService>.Instance);
}
