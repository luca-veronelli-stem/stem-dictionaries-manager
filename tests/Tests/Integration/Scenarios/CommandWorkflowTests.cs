using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Integration;

namespace Tests.Integration.Scenarios;

/// <summary>
/// Test E2E per il workflow completo dei comandi.
/// Verifica CRUD, enable/disable per device, parametri strutturati.
/// </summary>
public class CommandWorkflowTests : IntegrationTestBase
{
    #region Command CRUD Tests

    [Fact]
    public async Task FullWorkflow_CreateCommand_EnableDisablePerDevice()
    {
        // Setup
        var deviceRepo = new DeviceRepository(Context, NullLogger<RepositoryBase<DeviceEntity>>.Instance);
        var cmdRepo = new CommandRepository(Context, NullLogger<RepositoryBase<CommandEntity>>.Instance);
        var stateRepo = new CommandDeviceStateRepository(Context, NullLogger<RepositoryBase<CommandDeviceStateEntity>>.Instance);

        var device1 = new DeviceEntity { Name = "Eden-XP", MachineCode = 3 };
        var device2 = new DeviceEntity { Name = "Spark", MachineCode = 7 };
        await deviceRepo.AddAsync(device1);
        await deviceRepo.AddAsync(device2);

        var command = new CommandEntity
        {
            Name = "ReadVariable",
            CodeHigh = 0x00,
            CodeLow = 0x01,
            IsResponse = false,
            Parameters = []
        };
        await cmdRepo.AddAsync(command);

        // Default: comando abilitato per tutti (nessun override)
        IReadOnlyList<CommandDeviceStateEntity> states = await stateRepo.GetByDeviceIdAsync(device1.Id);
        Assert.Empty(states);

        // Disabilita per Eden-XP
        await stateRepo.AddAsync(new CommandDeviceStateEntity
        {
            CommandId = command.Id,
            DeviceId = device1.Id,
            IsEnabled = false
        });

        // Verifica
        CommandDeviceStateEntity? edenState = await stateRepo.GetByCommandAndDeviceAsync(command.Id, device1.Id);
        CommandDeviceStateEntity? sparkState = await stateRepo.GetByCommandAndDeviceAsync(command.Id, device2.Id);

        Assert.NotNull(edenState);
        Assert.False(edenState.IsEnabled);
        Assert.Null(sparkState); // Nessun override = default abilitato
    }

    #endregion

    #region Command Pair Tests

    [Fact]
    public async Task FullWorkflow_CommandPair_RequestAndResponse()
    {
        // Setup: coppia comando/risposta (stesso CodeLow)
        var cmdRepo = new CommandRepository(Context, NullLogger<RepositoryBase<CommandEntity>>.Instance);

        var request = new CommandEntity
        {
            Name = "ReadVariable",
            CodeHigh = 0x00, // Comando
            CodeLow = 0x01,
            IsResponse = false,
            Parameters = ["2|Address"]
        };
        var response = new CommandEntity
        {
            Name = "ReadVariableResponse",
            CodeHigh = 0x80, // Response
            CodeLow = 0x01, // Same CodeLow!
            IsResponse = true,
            Parameters = ["2|Address", "4|Value"]
        };
        await cmdRepo.AddAsync(request);
        await cmdRepo.AddAsync(response);

        // Verify: posso trovare la coppia per CodeLow
        List<CommandEntity> byCode = await Context.Commands
            .Where(c => c.CodeLow == 0x01)
            .ToListAsync();

        Assert.Equal(2, byCode.Count);
        Assert.Single(byCode, c => !c.IsResponse);
        Assert.Single(byCode, c => c.IsResponse);
    }

    #endregion

    #region Parameters Tests

    [Fact]
    public async Task FullWorkflow_CommandParameters_StructuredFormat()
    {
        // Setup: comando con parametri strutturati
        var cmdRepo = new CommandRepository(Context, NullLogger<RepositoryBase<CommandEntity>>.Instance);

        var command = new CommandEntity
        {
            Name = "WriteRegister",
            CodeHigh = 0x00,
            CodeLow = 0x10,
            IsResponse = false,
            Parameters = ["2|Indirizzo memoria", "4|Valore registro", "1|Flags"]
        };
        await cmdRepo.AddAsync(command);

        // Verify the typed list survives a real round-trip through SQLite via
        // the EF Core value conversion (this is the #7 conversion proof).
        Context.ChangeTracker.Clear();
        CommandEntity? loaded = await cmdRepo.GetByIdAsync(command.Id);
        Assert.NotNull(loaded);
        Assert.Equal(
            ["2|Indirizzo memoria", "4|Valore registro", "1|Flags"],
            loaded.Parameters);
    }

    #endregion
}
