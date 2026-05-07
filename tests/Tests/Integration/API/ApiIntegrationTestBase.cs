using Core.Enums;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Services;
using Services.Interfaces;

namespace Tests.Integration.API;

/// <summary>
/// Base class per integration tests degli endpoint API.
/// Configura DB SQLite in-memory + tutti i services.
/// Espone helper per seed dati realistici.
/// </summary>
public abstract class ApiIntegrationTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly AppDbContext Context;
    protected readonly IDeviceService DeviceService;
    protected readonly IBoardService BoardService;
    protected readonly IDictionaryService DictionaryService;
    protected readonly IVariableService VariableService;
    protected readonly ICommandService CommandService;

    protected ApiIntegrationTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();

        // Seed utente per audit
        Context.Users.Add(new UserEntity { Username = "test", DisplayName = "Test" });
        Context.SaveChanges();

        // Repositories
        var deviceRepo = new DeviceRepository(Context);
        var boardRepo = new BoardRepository(Context);
        var dictRepo = new DictionaryRepository(Context);
        var varRepo = new VariableRepository(Context);
        var commandRepo = new CommandRepository(Context);
        var auditRepo = new AuditEntryRepository(Context);
        var bitInterpRepo = new BitInterpretationRepository(Context);
        var cmdDeviceStateRepo = new CommandDeviceStateRepository(Context);
        var overrideRepo = new StandardVariableOverrideRepository(Context);

        // Services
        IAuditService auditService = new AuditService(auditRepo);
        ICurrentUserProvider userProvider = new CurrentUserProvider { CurrentUserId = 1 };

        DeviceService = new DeviceService(deviceRepo, boardRepo, dictRepo, auditService, userProvider);
        BoardService = new BoardService(boardRepo, dictRepo, auditService, userProvider);
        DictionaryService = new DictionaryService(dictRepo, varRepo, auditService, userProvider);
        VariableService = new VariableService(varRepo, dictRepo, bitInterpRepo, overrideRepo, auditService, userProvider);
        CommandService = new CommandService(commandRepo, cmdDeviceStateRepo, auditService, userProvider);
    }

    /// <summary>
    /// Seed scenario completo: Device + Board + Standard + Dizionario specifico + Variabili + Override.
    /// Ritorna (deviceId, boardId, standardDictId, specificDictId).
    /// </summary>
    protected async Task<(int deviceId, int boardId, int stdDictId, int specDictId)>
        SeedFullScenarioAsync()
    {
        // Device
        var device = await DeviceService.AddAsync(
            new Core.Models.Device("Optimus-XP", 10, "Piano oleodinamico"));

        // Standard dictionary + variabili
        var stdDict = await DictionaryService.AddAsync(
            new Core.Models.Dictionary("Standard", "Variabili comuni", isStandard: true));

        await DictionaryService.AddVariableAsync(stdDict.Id,
            new Core.Models.Variable("Firmware", 0x00, 0x00,
                DataTypeKind.UInt16, AccessMode.ReadOnly, "UInt16",
                description: "Versione firmware"));

        await DictionaryService.AddVariableAsync(stdDict.Id,
            new Core.Models.Variable("Matricola", 0x00, 0x03,
                DataTypeKind.UInt32, AccessMode.ReadWrite, "UInt32",
                description: "Numero matricola"));

        await DictionaryService.AddVariableAsync(stdDict.Id,
            new Core.Models.Variable("Deprecata", 0x00, 0x10,
                DataTypeKind.UInt8, AccessMode.ReadOnly, "UInt8",
                isEnabled: false, description: "Variabile deprecata"));

        // Dizionario specifico + variabili
        var specDict = await DictionaryService.AddAsync(
            new Core.Models.Dictionary("Optimus-XP", "Variabili Optimus"));

        await DictionaryService.AddVariableAsync(specDict.Id,
            new Core.Models.Variable("SystemOn", 0x80, 0x03,
                DataTypeKind.UInt8, AccessMode.ReadOnly, "UInt8",
                minValue: 0, maxValue: 1, description: "Piano acceso"));

        await DictionaryService.AddVariableAsync(specDict.Id,
            new Core.Models.Variable("Disabilitata", 0x80, 0x04,
                DataTypeKind.UInt8, AccessMode.ReadOnly, "UInt8",
                isEnabled: false, description: "Variabile disabilitata"));

        // Board
        var board = await BoardService.AddAsync(
            new Core.Models.Board(device.Id, "Madre", firmwareType: 5,
                boardNumber: 1, isPrimary: true, dictionaryId: specDict.Id,
                machineCode: 10));

        // Override: disabilita Matricola per questo dizionario
        var stdVars = await VariableService.GetByDictionaryIdAsync(stdDict.Id);
        var matricola = stdVars.First(v => v.Name == "Matricola");
        await VariableService.SetOverrideAsync(specDict.Id, matricola.Id,
            isEnabled: false, description: "Non usato su Optimus");

        return (device.Id, board.Id, stdDict.Id, specDict.Id);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
