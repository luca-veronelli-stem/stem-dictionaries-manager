using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per BoardService (Domain v2).
/// </summary>
public class BoardServiceTests : IntegrationTestBase
{
    private readonly BoardService _service;

    public BoardServiceTests()
    {
        var boardRepository = new BoardRepository(Context);
        var dictionaryRepository = new DictionaryRepository(Context);
        _service = new BoardService(boardRepository, dictionaryRepository);
    }

    public override async Task InitializeAsync()
    {
        await SeedTestDevicesAsync();
    }

    [Fact]
    public async Task AddAsync_ValidBoard_CreatesAndReturns()
    {
        // DeviceId=9 → Optimus-XP, MachineCode=10
        var board = new Board(9, "TestBoard", 17, 1, "DIS001", machineCode: 10);

        var result = await _service.AddAsync(board);

        Assert.True(result.Id > 0);
        Assert.Equal("TestBoard", result.Name);
        Assert.Equal(17, result.FirmwareType);
    }

    [Fact]
    public async Task AddAsync_WithDictionary_ValidatesExistence()
    {
        var dict = new DictionaryEntity { Name = "TestDict" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        var board = new Board(3, "Madre", 18, 1, dictionaryId: dict.Id, machineCode: 3);
        var result = await _service.AddAsync(board);

        Assert.Equal(dict.Id, result.DictionaryId);
    }

    [Fact]
    public async Task AddAsync_NonExistingDictionary_ThrowsInvalidOperationException()
    {
        var board = new Board(3, "Test", 18, 1, dictionaryId: 999, machineCode: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(board));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBoard_ReturnsBoard()
    {
        // DeviceId=6 → Spark, MachineCode=7
        var created = await _service.AddAsync(new Board(6, "FindMe", 20, 1, machineCode: 7));

        var result = await _service.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal("FindMe", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        Assert.Null(await _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByDeviceIdAsync_ReturnsMatchingBoards()
    {
        // DeviceId=10 → R3L-XP, MachineCode=11
        await _service.AddAsync(new Board(10, "R3L-1", 11, 1, machineCode: 11));
        await _service.AddAsync(new Board(10, "R3L-2", 12, 2, machineCode: 11));
        await _service.AddAsync(new Board(3, "Eden-1", 18, 1, machineCode: 3));

        var result = await _service.GetByDeviceIdAsync(10);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(10, b.DeviceId));
    }

    [Fact]
    public async Task GetByProtocolAddressAsync_ExistingAddress_ReturnsBoard()
    {
        // DeviceId=9 → Optimus-XP, MachineCode=10
        var board = new Board(9, "ByAddress", 17, 1, machineCode: 10);
        var created = await _service.AddAsync(board);

        var result = await _service.GetByProtocolAddressAsync(created.ProtocolAddress);

        Assert.NotNull(result);
        Assert.Equal("ByAddress", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ExistingBoard_UpdatesBoard()
    {
        var created = await _service.AddAsync(new Board(4, "Before", 5, 1, machineCode: 4));
        var updated = Board.Restore(created.Id, 4, "After", 5, 1, "NEW-PN", false, null, machineCode: 4);

        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("After", result!.Name);
        Assert.Equal("NEW-PN", result.PartNumber);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        var nonExisting = Board.Restore(999, 5, "Ghost", 20, 1, null, false, null, machineCode: 5);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(nonExisting));
    }

    [Fact]
    public async Task DeleteAsync_ExistingBoard_RemovesBoard()
    {
        var created = await _service.AddAsync(new Board(6, "Delete", 20, 1, machineCode: 7));

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
    public async Task GetAllAsync_ReturnsAllBoards()
    {
        await _service.AddAsync(new Board(1, "Board1", 20, 1, machineCode: 1));
        await _service.AddAsync(new Board(9, "Board2", 17, 1, machineCode: 10));

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    // === IsPrimary Validation Tests ===

    [Fact]
    public async Task AddAsync_IsPrimaryTrue_CreatesBoard()
    {
        var board = new Board(4, "Principale", 5, 1, isPrimary: true, machineCode: 4);

        var created = await _service.AddAsync(board);

        Assert.True(created.IsPrimary);
    }

    [Fact]
    public async Task AddAsync_SecondPrimarySameDevice_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Board(5, "HMI", 20, 1, isPrimary: true, machineCode: 5));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Board(5, "Display", 10, 2, isPrimary: true, machineCode: 5)));
    }

    [Fact]
    public async Task AddAsync_PrimaryOnDifferentDevices_IsAllowed()
    {
        await _service.AddAsync(new Board(5, "Spyke Main", 20, 1, isPrimary: true, machineCode: 5));

        var second = await _service.AddAsync(
            new Board(6, "Spark Main", 20, 1, isPrimary: true, machineCode: 7));

        Assert.True(second.IsPrimary);
    }

    [Fact]
    public async Task UpdateAsync_SetPrimaryWhenAnotherExists_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Board(8, "Display", 10, 1, isPrimary: true, machineCode: 9));
        var peripheral = await _service.AddAsync(
            new Board(8, "Periferica", 4, 2, machineCode: 9));

        var updated = Board.Restore(
            peripheral.Id, 8, "Periferica", 4, 2, null, true, null, machineCode: 9);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(updated));
    }

    [Fact]
    public async Task UpdateAsync_KeepSamePrimary_IsAllowed()
    {
        var primary = await _service.AddAsync(
            new Board(11, "Madre", 19, 1, isPrimary: true, machineCode: 12));

        var updated = Board.Restore(
            primary.Id, 11, "Madre Rinominata", 19, 1, null, true, null, machineCode: 12);

        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(primary.Id);
        Assert.Equal("Madre Rinominata", result!.Name);
        Assert.True(result.IsPrimary);
    }

    [Fact]
    public async Task AddAsync_NonPrimary_DoesNotConflict()
    {
        await _service.AddAsync(new Board(7, "Madre", 17, 1, isPrimary: true, machineCode: 8));

        var peripheral = await _service.AddAsync(
            new Board(7, "Tastiera", 4, 2, isPrimary: false, machineCode: 8));

        Assert.False(peripheral.IsPrimary);
    }

    // === Auto-assign Dictionary Tests (BR-021) ===

    [Fact]
    public async Task AddAsync_SameFirmwareType_AutoAssignsDictionary()
    {
        // Arrange: crea dizionario e board con FW=4 che lo referenzia
        var dict = new DictionaryEntity { Name = "Pulsantiere" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        await _service.AddAsync(new Board(3, "Pulsantiera 1", 4, 1,
            dictionaryId: dict.Id, machineCode: 3));

        // Act: nuova board FW=4 SENZA dizionario
        var newBoard = await _service.AddAsync(
            new Board(3, "Pulsantiera 2", 4, 2, machineCode: 3));

        // Assert: eredita il dizionario
        Assert.Equal(dict.Id, newBoard.DictionaryId);
    }

    [Fact]
    public async Task AddAsync_DifferentFirmwareType_DoesNotAutoAssign()
    {
        // Arrange: crea dizionario e board con FW=4
        var dict = new DictionaryEntity { Name = "Pulsantiere" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        await _service.AddAsync(new Board(3, "Pulsantiera", 4, 1,
            dictionaryId: dict.Id, machineCode: 3));

        // Act: nuova board FW=5 (diverso) senza dizionario
        var newBoard = await _service.AddAsync(
            new Board(3, "Madre", 5, 2, machineCode: 3));

        // Assert: NON eredita il dizionario
        Assert.Null(newBoard.DictionaryId);
    }

    // === Cascade Delete Dictionary Tests (BR-022) ===

    [Fact]
    public async Task DeleteAsync_DedicatedDictionary_DeletesDictionary()
    {
        // Arrange: dizionario usato da 1 sola board
        var dict = new DictionaryEntity { Name = "Eden-XP" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        var board = await _service.AddAsync(new Board(3, "Madre", 5, 1,
            dictionaryId: dict.Id, machineCode: 3));

        // Act
        await _service.DeleteAsync(board.Id);

        // Assert: dizionario eliminato
        var dictAfter = await Context.Dictionaries.FindAsync(dict.Id);
        Assert.Null(dictAfter);
    }

    [Fact]
    public async Task DeleteAsync_SharedDictionary_KeepsDictionary()
    {
        // Arrange: dizionario condiviso da 2 board
        var dict = new DictionaryEntity { Name = "Pulsantiere" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        var board1 = await _service.AddAsync(new Board(3, "Pulsantiera 1", 4, 1,
            dictionaryId: dict.Id, machineCode: 3));
        await _service.AddAsync(new Board(3, "Pulsantiera 2", 4, 2,
            dictionaryId: dict.Id, machineCode: 3));

        // Act: elimina solo la prima
        await _service.DeleteAsync(board1.Id);

        // Assert: dizionario ancora presente
        var dictAfter = await Context.Dictionaries.FindAsync(dict.Id);
        Assert.NotNull(dictAfter);
    }
}
