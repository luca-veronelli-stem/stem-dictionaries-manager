using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per BoardRepository.
/// </summary>
public class BoardRepositoryTests : IntegrationTestBase
{
    private readonly BoardRepository _repository;
    private BoardTypeEntity _testBoardType = null!;

    public BoardRepositoryTests()
    {
        _repository = new BoardRepository(Context);
        SetupBoardType().Wait();
    }

    private async Task SetupBoardType()
    {
        _testBoardType = new BoardTypeEntity { Name = "Madre", FirmwareType = 17 };
        Context.BoardTypes.Add(_testBoardType);
        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_CreatesBoard()
    {
        var board = new BoardEntity
        {
            DeviceType = DeviceType.OptimusXp,
            BoardTypeId = _testBoardType.Id,
            Name = "Madre",
            BoardNumber = 1,
            ProtocolAddress = 0x000A0441,
            PartNumber = "DIS0020477"
        };

        var result = await _repository.AddAsync(board);

        Assert.True(result.Id > 0);
        Assert.Equal("Madre", result.Name);
        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBoard_WithBoardType()
    {
        var board = new BoardEntity
        {
            DeviceType = DeviceType.Eden,
            BoardTypeId = _testBoardType.Id,
            Name = "TestBoard",
            BoardNumber = 1,
            ProtocolAddress = 0x00030441
        };
        await _repository.AddAsync(board);

        var result = await _repository.GetByIdAsync(board.Id);

        Assert.NotNull(result);
        Assert.Equal("TestBoard", result.Name);
        Assert.NotNull(result.BoardType);
        Assert.Equal("Madre", result.BoardType.Name);
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
            DeviceType = DeviceType.OptimusXp,
            BoardTypeId = _testBoardType.Id,
            Name = "Board1",
            BoardNumber = 1,
            ProtocolAddress = 0x000A0441
        });
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.OptimusXp,
            BoardTypeId = _testBoardType.Id,
            Name = "Board2",
            BoardNumber = 2,
            ProtocolAddress = 0x000A0442
        });
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.Eden,
            BoardTypeId = _testBoardType.Id,
            Name = "EdenBoard",
            BoardNumber = 1,
            ProtocolAddress = 0x00030441
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
    public async Task GetByDeviceTypeAsync_IncludesBoardType()
    {
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.R3lXp,
            BoardTypeId = _testBoardType.Id,
            Name = "R3lBoard",
            BoardNumber = 1,
            ProtocolAddress = 0x000B0441
        });

        var result = await _repository.GetByDeviceTypeAsync(DeviceType.R3lXp);

        Assert.Single(result);
        Assert.NotNull(result[0].BoardType);
        Assert.Equal("Madre", result[0].BoardType.Name);
    }

    [Fact]
    public async Task GetByProtocolAddressAsync_ReturnsBoard()
    {
        var board = new BoardEntity
        {
            DeviceType = DeviceType.Spark,
            BoardTypeId = _testBoardType.Id,
            Name = "SparkBoard",
            BoardNumber = 1,
            ProtocolAddress = 0x00070441
        };
        await _repository.AddAsync(board);

        var result = await _repository.GetByProtocolAddressAsync(0x00070441);

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
    public async Task GetByProtocolAddressAsync_IncludesBoardType()
    {
        await _repository.AddAsync(new BoardEntity
        {
            DeviceType = DeviceType.Gradino,
            BoardTypeId = _testBoardType.Id,
            Name = "GradinoBoard",
            BoardNumber = 1,
            ProtocolAddress = 0x00040441
        });

        var result = await _repository.GetByProtocolAddressAsync(0x00040441);

        Assert.NotNull(result);
        Assert.NotNull(result.BoardType);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBoard()
    {
        var board = new BoardEntity
        {
            DeviceType = DeviceType.BleModule,
            BoardTypeId = _testBoardType.Id,
            Name = "ToDelete",
            BoardNumber = 1,
            ProtocolAddress = 0x00060441
        };
        await _repository.AddAsync(board);

        await _repository.DeleteAsync(board.Id);

        var result = await _repository.GetByIdAsync(board.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }
}
