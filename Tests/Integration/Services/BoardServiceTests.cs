using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per BoardService.
/// </summary>
public class BoardServiceTests : IntegrationTestBase
{
    private readonly BoardService _service;
    private BoardTypeEntity _testBoardType = null!;

    public BoardServiceTests()
    {
        var boardRepository = new BoardRepository(Context);
        var boardTypeRepository = new BoardTypeRepository(Context);
        _service = new BoardService(boardRepository, boardTypeRepository);
    }

    public override async Task InitializeAsync()
    {
        _testBoardType = new BoardTypeEntity { Name = "Madre", FirmwareType = 17 };
        Context.BoardTypes.Add(_testBoardType);
        await Context.SaveChangesAsync();
    }

    // === BoardType Tests ===

    [Fact]
    public async Task AddBoardTypeAsync_ValidBoardType_CreatesAndReturns()
    {
        // Arrange
        var boardType = new BoardType("Pulsantiera", 4);

        // Act
        var result = await _service.AddBoardTypeAsync(boardType);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("Pulsantiera", result.Name);
        Assert.Equal(4, result.FirmwareType);
    }

    [Fact]
    public async Task AddBoardTypeAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddBoardTypeAsync(new BoardType("DupName", 100));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddBoardTypeAsync(new BoardType("DupName", 101)));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddBoardTypeAsync_DuplicateFirmwareType_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddBoardTypeAsync(new BoardType("First", 50));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddBoardTypeAsync(new BoardType("Second", 50)));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GetBoardTypesAsync_ReturnsAllBoardTypes()
    {
        // Arrange (uno già creato in setup)
        await _service.AddBoardTypeAsync(new BoardType("Extra", 200));

        // Act
        var result = await _service.GetBoardTypesAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetBoardTypeByNameAsync_ExistingName_ReturnsBoardType()
    {
        var result = await _service.GetBoardTypeByNameAsync("Madre");

        Assert.NotNull(result);
        Assert.Equal(17, result.FirmwareType);
    }

    [Fact]
    public async Task GetBoardTypeByFirmwareTypeAsync_ExistingType_ReturnsBoardType()
    {
        var result = await _service.GetBoardTypeByFirmwareTypeAsync(17);

        Assert.NotNull(result);
        Assert.Equal("Madre", result.Name);
    }

    // === Board Tests ===

    [Fact]
    public async Task AddAsync_ValidBoard_CreatesAndReturns()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var board = new Board(DeviceType.OptimusXp, boardType, "TestBoard", 1, "DIS001");

        // Act
        var result = await _service.AddAsync(board);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("TestBoard", result.Name);
        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
        Assert.NotNull(result.BoardType);
    }

    [Fact]
    public async Task AddAsync_NonExistingBoardType_ThrowsInvalidOperationException()
    {
        // Arrange
        var fakeBoardType = BoardType.Restore(999, "Fake", 999);
        var board = new Board(DeviceType.EdenXp, fakeBoardType, "Test", 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(board));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBoard_ReturnsBoard()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var created = await _service.AddAsync(new Board(DeviceType.Spark, boardType, "FindMe", 1));

        // Act
        var result = await _service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FindMe", result.Name);
        Assert.NotNull(result.BoardType);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByDeviceTypeAsync_ReturnsMatchingBoards()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Board(DeviceType.R3lXp, boardType, "R3L-1", 1));
        await _service.AddAsync(new Board(DeviceType.R3lXp, boardType, "R3L-2", 2));
        await _service.AddAsync(new Board(DeviceType.EdenXp, boardType, "Eden-1", 1));

        // Act
        var result = await _service.GetByDeviceTypeAsync(DeviceType.R3lXp);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(DeviceType.R3lXp, b.DeviceType));
    }

    [Fact]
    public async Task GetByProtocolAddressAsync_ExistingAddress_ReturnsBoard()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var board = new Board(DeviceType.OptimusXp, boardType, "ByAddress", 1);
        var created = await _service.AddAsync(board);

        // Act
        var result = await _service.GetByProtocolAddressAsync(created.ProtocolAddress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ByAddress", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ExistingBoard_UpdatesBoard()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var created = await _service.AddAsync(new Board(DeviceType.Gradino, boardType, "Before", 1));
        var updated = Board.Restore(created.Id, DeviceType.Gradino, boardType, "After", 1, "NEW-PN", false);

        // Act
        await _service.UpdateAsync(updated);

        // Assert
        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("After", result!.Name);
        Assert.Equal("NEW-PN", result.PartNumber);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var nonExisting = Board.Restore(999, DeviceType.Spyke, boardType, "Ghost", 1, null, false);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(nonExisting));
    }

    [Fact]
    public async Task DeleteAsync_ExistingBoard_RemovesBoard()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var created = await _service.AddAsync(new Board(DeviceType.Spark, boardType, "Delete", 1));

        // Act
        await _service.DeleteAsync(created.Id);

        // Assert
        var result = await _service.GetByIdAsync(created.Id);
        Assert.Null(result);
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
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Board(DeviceType.SherpaSlim, boardType, "Board1", 1));
        await _service.AddAsync(new Board(DeviceType.OptimusXp, boardType, "Board2", 1));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    // === IsPrimary Validation Tests ===

    [Fact]
    public async Task AddAsync_IsPrimaryTrue_CreatesBoard()
    {
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var board = new Board(DeviceType.Gradino, boardType, "Principale", 1, isPrimary: true);

        var created = await _service.AddAsync(board);

        Assert.True(created.IsPrimary);
    }

    [Fact]
    public async Task AddAsync_SecondPrimarySameDevice_ThrowsInvalidOperationException()
    {
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Board(DeviceType.Spyke, boardType, "HMI", 1, isPrimary: true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Board(DeviceType.Spyke, boardType, "Display", 2, isPrimary: true)));
    }

    [Fact]
    public async Task AddAsync_PrimaryOnDifferentDevices_IsAllowed()
    {
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Board(DeviceType.Spyke, boardType, "Spyke Main", 1, isPrimary: true));

        var second = await _service.AddAsync(
            new Board(DeviceType.Spark, boardType, "Spark Main", 1, isPrimary: true));

        Assert.True(second.IsPrimary);
    }

    [Fact]
    public async Task UpdateAsync_SetPrimaryWhenAnotherExists_ThrowsInvalidOperationException()
    {
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Board(DeviceType.O3zTech, boardType, "Display", 1, isPrimary: true));
        var peripheral = await _service.AddAsync(
            new Board(DeviceType.O3zTech, boardType, "Periferica", 2));

        var updated = Board.Restore(
            peripheral.Id, DeviceType.O3zTech, boardType, "Periferica", 2, null, true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(updated));
    }

    [Fact]
    public async Task UpdateAsync_KeepSamePrimary_IsAllowed()
    {
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var primary = await _service.AddAsync(
            new Board(DeviceType.EdenBs8, boardType, "Madre", 1, isPrimary: true));

        var updated = Board.Restore(
            primary.Id, DeviceType.EdenBs8, boardType, "Madre Rinominata", 1, null, true);

        await _service.UpdateAsync(updated);

        var result = await _service.GetByIdAsync(primary.Id);
        Assert.Equal("Madre Rinominata", result!.Name);
        Assert.True(result.IsPrimary);
    }

    [Fact]
    public async Task AddAsync_NonPrimary_DoesNotConflict()
    {
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Board(DeviceType.TopLiftA2, boardType, "Madre", 1, isPrimary: true));

        var peripheral = await _service.AddAsync(
            new Board(DeviceType.TopLiftA2, boardType, "Tastiera", 2, isPrimary: false));

        Assert.False(peripheral.IsPrimary);
    }
}
