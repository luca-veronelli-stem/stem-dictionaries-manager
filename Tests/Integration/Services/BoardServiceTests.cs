using Core.Enums;
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

    [Fact]
    public async Task AddAsync_ValidBoard_CreatesAndReturns()
    {
        var board = new Board(DeviceType.OptimusXp, "TestBoard", 17, 1, "DIS001");

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

        var board = new Board(DeviceType.EdenXp, "Madre", 18, 1, dictionaryId: dict.Id);
        var result = await _service.AddAsync(board);

        Assert.Equal(dict.Id, result.DictionaryId);
    }

    [Fact]
    public async Task AddAsync_NonExistingDictionary_ThrowsInvalidOperationException()
    {
        var board = new Board(DeviceType.EdenXp, "Test", 18, 1, dictionaryId: 999);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(board));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBoard_ReturnsBoard()
    {
        var created = await _service.AddAsync(new Board(DeviceType.Spark, "FindMe", 20, 1));

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
    public async Task GetByDeviceTypeAsync_ReturnsMatchingBoards()
    {
        await _service.AddAsync(new Board(DeviceType.R3lXp, "R3L-1", 11, 1));
        await _service.AddAsync(new Board(DeviceType.R3lXp, "R3L-2", 12, 2));
        await _service.AddAsync(new Board(DeviceType.EdenXp, "Eden-1", 18, 1));

        var result = await _service.GetByDeviceTypeAsync(DeviceType.R3lXp);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(DeviceType.R3lXp, b.DeviceType));
    }

    [Fact]
    public async Task GetByProtocolAddressAsync_ExistingAddress_ReturnsBoard()
    {
        var board = new Board(DeviceType.OptimusXp, "ByAddress", 17, 1);
        var created = await _service.AddAsync(board);

        var result = await _service.GetByProtocolAddressAsync(created.ProtocolAddress);

        Assert.NotNull(result);
        Assert.Equal("ByAddress", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ExistingBoard_UpdatesBoard()
    {
        var created = await _service.AddAsync(new Board(DeviceType.Gradino, "Before", 5, 1));
        var updated = Board.Restore(created.Id, DeviceType.Gradino, "After", 5, 1, "NEW-PN", false, null);

        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("After", result!.Name);
        Assert.Equal("NEW-PN", result.PartNumber);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        var nonExisting = Board.Restore(999, DeviceType.Spyke, "Ghost", 20, 1, null, false, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(nonExisting));
    }

    [Fact]
    public async Task DeleteAsync_ExistingBoard_RemovesBoard()
    {
        var created = await _service.AddAsync(new Board(DeviceType.Spark, "Delete", 20, 1));

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
        await _service.AddAsync(new Board(DeviceType.SherpaSlim, "Board1", 20, 1));
        await _service.AddAsync(new Board(DeviceType.OptimusXp, "Board2", 17, 1));

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    // === IsPrimary Validation Tests ===

    [Fact]
    public async Task AddAsync_IsPrimaryTrue_CreatesBoard()
    {
        var board = new Board(DeviceType.Gradino, "Principale", 5, 1, isPrimary: true);

        var created = await _service.AddAsync(board);

        Assert.True(created.IsPrimary);
    }

    [Fact]
    public async Task AddAsync_SecondPrimarySameDevice_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Board(DeviceType.Spyke, "HMI", 20, 1, isPrimary: true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Board(DeviceType.Spyke, "Display", 10, 2, isPrimary: true)));
    }

    [Fact]
    public async Task AddAsync_PrimaryOnDifferentDevices_IsAllowed()
    {
        await _service.AddAsync(new Board(DeviceType.Spyke, "Spyke Main", 20, 1, isPrimary: true));

        var second = await _service.AddAsync(
            new Board(DeviceType.Spark, "Spark Main", 20, 1, isPrimary: true));

        Assert.True(second.IsPrimary);
    }

    [Fact]
    public async Task UpdateAsync_SetPrimaryWhenAnotherExists_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Board(DeviceType.O3zTech, "Display", 10, 1, isPrimary: true));
        var peripheral = await _service.AddAsync(
            new Board(DeviceType.O3zTech, "Periferica", 4, 2));

        var updated = Board.Restore(
            peripheral.Id, DeviceType.O3zTech, "Periferica", 4, 2, null, true, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(updated));
    }

    [Fact]
    public async Task UpdateAsync_KeepSamePrimary_IsAllowed()
    {
        var primary = await _service.AddAsync(
            new Board(DeviceType.EdenBs8, "Madre", 19, 1, isPrimary: true));

        var updated = Board.Restore(
            primary.Id, DeviceType.EdenBs8, "Madre Rinominata", 19, 1, null, true, null);

        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(primary.Id);
        Assert.Equal("Madre Rinominata", result!.Name);
        Assert.True(result.IsPrimary);
    }

    [Fact]
    public async Task AddAsync_NonPrimary_DoesNotConflict()
    {
        await _service.AddAsync(new Board(DeviceType.TopLiftA2, "Madre", 17, 1, isPrimary: true));

        var peripheral = await _service.AddAsync(
            new Board(DeviceType.TopLiftA2, "Tastiera", 4, 2, isPrimary: false));

        Assert.False(peripheral.IsPrimary);
    }
}
