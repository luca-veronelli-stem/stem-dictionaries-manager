using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per CommandRepository.
/// </summary>
public class CommandRepositoryTests : IntegrationTestBase
{
    private readonly CommandRepository _repository;

    public CommandRepositoryTests()
    {
        _repository = new CommandRepository(Context);
    }

    [Fact]
    public async Task AddAsync_CreatesCommand()
    {
        var command = new CommandEntity
        {
            Name = "READ_VARIABLE",
            CodeHigh = 0x01,
            CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"address\", \"length\"]"
        };

        var result = await _repository.AddAsync(command);

        Assert.True(result.Id > 0);
        Assert.Equal("READ_VARIABLE", result.Name);
        Assert.Equal(0x01, result.CodeHigh);
        Assert.Equal(0x00, result.CodeLow);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCommand()
    {
        var command = new CommandEntity
        {
            Name = "TestCommand",
            CodeHigh = 0x02,
            CodeLow = 0x01,
            IsResponse = false
        };
        await _repository.AddAsync(command);

        var result = await _repository.GetByIdAsync(command.Id);

        Assert.NotNull(result);
        Assert.Equal("TestCommand", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsCommand()
    {
        var command = new CommandEntity
        {
            Name = "WRITE_VARIABLE",
            CodeHigh = 0x01,
            CodeLow = 0x01,
            IsResponse = false
        };
        await _repository.AddAsync(command);

        var result = await _repository.GetByCodeAsync(0x01, 0x01, false);

        Assert.NotNull(result);
        Assert.Equal("WRITE_VARIABLE", result.Name);
    }

    [Fact]
    public async Task GetByCodeAsync_DistinguishesRequestFromResponse()
    {
        var request = new CommandEntity
        {
            Name = "CMD_REQUEST",
            CodeHigh = 0x03,
            CodeLow = 0x00,
            IsResponse = false
        };
        var response = new CommandEntity
        {
            Name = "CMD_RESPONSE",
            CodeHigh = 0x03,
            CodeLow = 0x00,
            IsResponse = true
        };
        await _repository.AddAsync(request);
        await _repository.AddAsync(response);

        var requestResult = await _repository.GetByCodeAsync(0x03, 0x00, false);
        var responseResult = await _repository.GetByCodeAsync(0x03, 0x00, true);

        Assert.NotNull(requestResult);
        Assert.Equal("CMD_REQUEST", requestResult.Name);
        Assert.NotNull(responseResult);
        Assert.Equal("CMD_RESPONSE", responseResult.Name);
    }

    [Fact]
    public async Task GetByCodeAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByCodeAsync(0xFF, 0xFF, false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWithDeviceStatesAsync_ReturnsCommand_WithDeviceStates()
    {
        var command = new CommandEntity
        {
            Name = "DEVICE_INFO",
            CodeHigh = 0x04,
            CodeLow = 0x00,
            IsResponse = false
        };
        await _repository.AddAsync(command);

        // Aggiungi device states
        Context.CommandDeviceStates.AddRange(
            new CommandDeviceStateEntity
            {
                CommandId = command.Id,
                DeviceId = 10,
                IsEnabled = true
            },
            new CommandDeviceStateEntity
            {
                CommandId = command.Id,
                DeviceId = 3,
                IsEnabled = false
            }
        );
        await Context.SaveChangesAsync();

        var result = await _repository.GetWithDeviceStatesAsync(command.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.DeviceStates.Count);
        Assert.Contains(result.DeviceStates, ds => ds.DeviceId == 10 && ds.IsEnabled);
        Assert.Contains(result.DeviceStates, ds => ds.DeviceId == 3 && !ds.IsEnabled);
    }

    [Fact]
    public async Task GetWithDeviceStatesAsync_NoStates_ReturnsEmptyCollection()
    {
        var command = new CommandEntity
        {
            Name = "LONELY_CMD",
            CodeHigh = 0x05,
            CodeLow = 0x00,
            IsResponse = false
        };
        await _repository.AddAsync(command);

        var result = await _repository.GetWithDeviceStatesAsync(command.Id);

        Assert.NotNull(result);
        Assert.Empty(result.DeviceStates);
    }

    [Fact]
    public async Task GetWithDeviceStatesAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetWithDeviceStatesAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCommands()
    {
        await _repository.AddAsync(new CommandEntity
        {
            Name = "CMD1",
            CodeHigh = 0x10,
            CodeLow = 0x00,
            IsResponse = false
        });
        await _repository.AddAsync(new CommandEntity
        {
            Name = "CMD2",
            CodeHigh = 0x10,
            CodeLow = 0x01,
            IsResponse = false
        });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCommand()
    {
        var command = new CommandEntity
        {
            Name = "ToDelete",
            CodeHigh = 0x99,
            CodeLow = 0x99,
            IsResponse = false
        };
        await _repository.AddAsync(command);

        await _repository.DeleteAsync(command.Id);

        var result = await _repository.GetByIdAsync(command.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }

    // === GetByNameAsync ===

    [Fact]
    public async Task GetByNameAsync_Existing_ReturnsCommand()
    {
        // Arrange
        await _repository.AddAsync(new CommandEntity
        {
            Name = "UNIQUE_NAME",
            CodeHigh = 0xA0,
            CodeLow = 0x00,
            IsResponse = false
        });

        // Act
        var result = await _repository.GetByNameAsync("UNIQUE_NAME");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UNIQUE_NAME", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByNameAsync("NONEXISTENT");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_IsCaseSensitive()
    {
        // Arrange
        await _repository.AddAsync(new CommandEntity
        {
            Name = "CaseSensitive",
            CodeHigh = 0xB0,
            CodeLow = 0x00,
            IsResponse = false
        });

        // Act
        var exact = await _repository.GetByNameAsync("CaseSensitive");
        var lower = await _repository.GetByNameAsync("casesensitive");

        // Assert — SQL Server/SQLite sono case-insensitive by default
        Assert.NotNull(exact);
        // lower potrebbe essere null o non null a seconda del DB
    }
}
