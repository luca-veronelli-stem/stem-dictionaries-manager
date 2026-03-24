using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per BoardRepository (Domain v2).
/// </summary>
public class BoardRepositoryTests : IntegrationTestBase
{
    private readonly BoardRepository _repository;

    public BoardRepositoryTests()
    {
        _repository = new BoardRepository(Context);
    }

    [Fact]
    public async Task AddAsync_CreatesBoard()
    {
        var board = new BoardEntity
        {
            DeviceType = DeviceType.OptimusXp,
            Name = "Madre",
            FirmwareType = 17,
            BoardNumber = 1,
            ProtocolAddress = 0x000A0441,
            PartNumber = "DIS0020477"
        };

        var result = await _repository.AddAsync(board);

        Assert.True(result.Id > 0);
        Assert.Equal("Madre", result.Name);
        Assert.Equal(17, result.FirmwareType);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBoard()
    {
        var board = new BoardEntity
        {
            DeviceType = DeviceType.EdenXp,
            Name = "TestBoard",
            FirmwareType = 18,
            BoardNumber = 1,
            ProtocolAddress = 0x00030441
        };
        await _repository.AddAsync(board);

        var result = await _repository.GetByIdAsync(board.Id);

        Assert.NotNull(result);
        Assert.Equal("TestBoard", result.Name);
        Assert.Equal(18, result.FirmwareType);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByDeviceTypeAsync_ReturnsMatchingBoards()
    {
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.OptimusXp, Name = "Board1",
            FirmwareType = 17, BoardNumber = 1, ProtocolAddress = 0x000A0441
        });
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.OptimusXp, Name = "Board2",
            FirmwareType = 4, BoardNumber = 2, ProtocolAddress = 0x000A0102
        });
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.EdenXp, Name = "EdenBoard",
            FirmwareType = 18, BoardNumber = 1, ProtocolAddress = 0x00030481
        });

        var result = await _repository.GetByDeviceTypeAsync(DeviceType.OptimusXp);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(DeviceType.OptimusXp, b.DeviceType));
    }

    [Fact]
    public async Task GetByDeviceTypeAsync_NoMatch_ReturnsEmptyList()
    {
        var result = await _repository.GetByDeviceTypeAsync(DeviceType.Spyke);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByProtocolAddressAsync_ReturnsBoard()
    {
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.Spark, Name = "SparkBoard",
            FirmwareType = 20, BoardNumber = 1, ProtocolAddress = 0x00070501
        });

        var result = await _repository.GetByProtocolAddressAsync(0x00070501);

        Assert.NotNull(result);
        Assert.Equal("SparkBoard", result.Name);
    }

    [Fact]
    public async Task GetByProtocolAddressAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByProtocolAddressAsync(0xFFFFFFFF);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithDictionary_IncludesNavigation()
    {
        var dict = new DictionaryEntity { Name = "TestDict" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();

        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.R3lXp, Name = "R3lBoard",
            FirmwareType = 11, BoardNumber = 1, ProtocolAddress = 0x000B02C1,
            DictionaryId = dict.Id
        });

        var boards = await _repository.GetByDeviceTypeAsync(DeviceType.R3lXp);

        Assert.Single(boards);
        Assert.NotNull(boards[0].Dictionary);
        Assert.Equal("TestDict", boards[0].Dictionary!.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBoard()
    {
        var board = new BoardEntity
        {
            DeviceType = DeviceType.Spark, Name = "ToDelete",
            FirmwareType = 20, BoardNumber = 1, ProtocolAddress = 0x00060501
        };
        await _repository.AddAsync(board);

        await _repository.DeleteAsync(board.Id);

        Assert.Null(await _repository.GetByIdAsync(board.Id));
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }
}
