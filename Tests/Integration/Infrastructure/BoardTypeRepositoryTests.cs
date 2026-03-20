using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per BoardTypeRepository.
/// </summary>
public class BoardTypeRepositoryTests : IntegrationTestBase
{
    private readonly BoardTypeRepository _repository;

    public BoardTypeRepositoryTests()
    {
        _repository = new BoardTypeRepository(Context);
    }

    [Fact]
    public async Task AddAsync_CreatesBoardType()
    {
        var boardType = new BoardTypeEntity
        {
            Name = "Madre",
            FirmwareType = 17
        };

        var result = await _repository.AddAsync(boardType);

        Assert.True(result.Id > 0);
        Assert.Equal("Madre", result.Name);
        Assert.Equal(17, result.FirmwareType);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBoardType()
    {
        var boardType = new BoardTypeEntity { Name = "Test", FirmwareType = 1 };
        await _repository.AddAsync(boardType);

        var result = await _repository.GetByIdAsync(boardType.Id);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsBoardType()
    {
        var boardType = new BoardTypeEntity { Name = "Pulsantiera", FirmwareType = 4 };
        await _repository.AddAsync(boardType);

        var result = await _repository.GetByNameAsync("Pulsantiera");

        Assert.NotNull(result);
        Assert.Equal(4, result.FirmwareType);
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByNameAsync("NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByFirmwareTypeAsync_ExistingType_ReturnsBoardType()
    {
        var boardType = new BoardTypeEntity { Name = "R3lXpMaster", FirmwareType = 25 };
        await _repository.AddAsync(boardType);

        var result = await _repository.GetByFirmwareTypeAsync(25);

        Assert.NotNull(result);
        Assert.Equal("R3lXpMaster", result.Name);
    }

    [Fact]
    public async Task GetByFirmwareTypeAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByFirmwareTypeAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBoardTypes()
    {
        await _repository.AddAsync(new BoardTypeEntity { Name = "Type1", FirmwareType = 1 });
        await _repository.AddAsync(new BoardTypeEntity { Name = "Type2", FirmwareType = 2 });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBoardType()
    {
        var boardType = new BoardTypeEntity { Name = "ToDelete", FirmwareType = 99 };
        await _repository.AddAsync(boardType);

        await _repository.DeleteAsync(boardType.Id);

        var result = await _repository.GetByIdAsync(boardType.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }
}
