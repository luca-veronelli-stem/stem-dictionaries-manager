using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services;
using Services.Interfaces;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per DeviceService.
/// </summary>
public class DeviceServiceTests : IntegrationTestBase
{
    private readonly DeviceService _service;

    public DeviceServiceTests()
    {
        SeedTestUser();
        var repository = new DeviceRepository(Context);
        var boardRepository = new BoardRepository(Context);
        var dictionaryRepository = new DictionaryRepository(Context);
        var auditRepository = new AuditEntryRepository(Context);
        IAuditService auditService = new AuditService(auditRepository);
        ICurrentUserProvider userProvider = new CurrentUserProvider { CurrentUserId = 1 };
        _service = new DeviceService(
            repository, boardRepository, dictionaryRepository, auditService, userProvider);
    }

    [Fact]
    public async Task AddAsync_ValidDevice_CreatesAndReturns()
    {
        var device = new Device("Eden-XP", 3, "Supporto barella");

        var result = await _service.AddAsync(device);

        Assert.True(result.Id > 0);
        Assert.Equal("Eden-XP", result.Name);
        Assert.Equal(3, result.MachineCode);
        Assert.Equal("Supporto barella", result.Description);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Device("Spark", 7));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Device("Spark", 99)));
    }

    [Fact]
    public async Task AddAsync_DuplicateMachineCode_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Device("Device A", 10));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Device("Device B", 10)));
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDevice()
    {
        var created = await _service.AddAsync(new Device("Spyke", 5));

        var result = await _service.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal("Spyke", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        Assert.Null(await _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDevices()
    {
        await _service.AddAsync(new Device("A", 1));
        await _service.AddAsync(new Device("B", 2));
        await _service.AddAsync(new Device("C", 3));

        var result = await _service.GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingDevice_UpdatesProperties()
    {
        var created = await _service.AddAsync(new Device("Before", 50));

        var updated = Device.Restore(created.Id, "After", 51, "New desc");
        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("After", result!.Name);
        Assert.Equal(51, result.MachineCode);
        Assert.Equal("New desc", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        var nonExisting = Device.Restore(999, "Ghost", 99, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(nonExisting));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Device("Existing", 1));
        var created = await _service.AddAsync(new Device("ToUpdate", 2));

        var conflicting = Device.Restore(created.Id, "Existing", 2, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(conflicting));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateMachineCode_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Device("Device A", 10));
        var created = await _service.AddAsync(new Device("Device B", 20));

        var conflicting = Device.Restore(created.Id, "Device B", 10, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(conflicting));
    }

    [Fact]
    public async Task UpdateAsync_KeepSameName_IsAllowed()
    {
        var created = await _service.AddAsync(new Device("KeepName", 30));

        var updated = Device.Restore(created.Id, "KeepName", 31, "Updated");
        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal(31, result!.MachineCode);
    }

    [Fact]
    public async Task UpdateAsync_KeepSameMachineCode_IsAllowed()
    {
        var created = await _service.AddAsync(new Device("Original", 40));

        var updated = Device.Restore(created.Id, "Renamed", 40, null);
        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("Renamed", result!.Name);
    }

    [Fact]
    public async Task DeleteAsync_ExistingDevice_RemovesIt()
    {
        var created = await _service.AddAsync(new Device("ToDelete", 77));

        await _service.DeleteAsync(created.Id);

        Assert.Null(await _service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(999));
    }

    [Fact]
    public async Task GetByNameAsync_Found_ReturnsDevice()
    {
        await _service.AddAsync(new Device("R3L-XP", 11));

        var result = await _service.GetByNameAsync("R3L-XP");

        Assert.NotNull(result);
        Assert.Equal(11, result.MachineCode);
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        Assert.Null(await _service.GetByNameAsync("NonExistent"));
    }

    // === Cascade Delete Tests (BR-023) ===

    [Fact]
    public async Task DeleteAsync_WithBoardsAndDedicatedDict_DeletesDictionary()
    {
        // Arrange: device con 1 board che ha un dizionario dedicato
        var device = await _service.AddAsync(new Device("TestDevice", 77));
        var dict = new DictionaryEntity { Name = "TestDict" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        Context.Boards.Add(new BoardEntity
        {
            DeviceId = device.Id,
            Name = "Madre",
            FirmwareType = 5,
            BoardNumber = 1,
            DictionaryId = dict.Id,
            ProtocolAddress = Board.CalculateAddress(77, 5, 1)
        });
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(device.Id);

        // Assert: device, board e dizionario eliminati
        Assert.Null(await _service.GetByIdAsync(device.Id));
        Assert.Null(await Context.Dictionaries.FindAsync(dict.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithSharedDict_KeepsDictionary()
    {
        // Arrange: 2 device, dizionario condiviso
        var device1 = await _service.AddAsync(new Device("Device1", 77));
        var device2 = await _service.AddAsync(new Device("Device2", 78));
        var dict = new DictionaryEntity { Name = "Pulsantiere" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        Context.Boards.AddRange(
            new BoardEntity
            {
                DeviceId = device1.Id, Name = "Puls 1", FirmwareType = 4,
                BoardNumber = 1, DictionaryId = dict.Id,
                ProtocolAddress = Board.CalculateAddress(77, 4, 1)
            },
            new BoardEntity
            {
                DeviceId = device2.Id, Name = "Puls 1", FirmwareType = 4,
                BoardNumber = 1, DictionaryId = dict.Id,
                ProtocolAddress = Board.CalculateAddress(78, 4, 1)
            });
        await Context.SaveChangesAsync();

        // Act: elimina solo device1
        await _service.DeleteAsync(device1.Id);

        // Assert: dizionario condiviso sopravvive
        Assert.Null(await _service.GetByIdAsync(device1.Id));
        Assert.NotNull(await Context.Dictionaries.FindAsync(dict.Id));
    }

    // === GetNextAvailableMachineCodeAsync ===

    [Fact]
    public async Task GetNextAvailableMachineCodeAsync_NoDevices_Returns1()
    {
        var next = await _service.GetNextAvailableMachineCodeAsync();

        Assert.Equal(1, next);
    }

    [Fact]
    public async Task GetNextAvailableMachineCodeAsync_WithDevices_ReturnsMaxPlusOne()
    {
        await _service.AddAsync(new Device("A", 3));
        await _service.AddAsync(new Device("B", 7));

        var next = await _service.GetNextAvailableMachineCodeAsync();

        Assert.Equal(8, next);
    }

    [Fact]
    public async Task GetNextAvailableMachineCodeAsync_SkipsReserved6()
    {
        await _service.AddAsync(new Device("A", 5));

        var next = await _service.GetNextAvailableMachineCodeAsync();

        // 5 + 1 = 6 riservato BLE → salta a 7
        Assert.Equal(7, next);
    }
}
