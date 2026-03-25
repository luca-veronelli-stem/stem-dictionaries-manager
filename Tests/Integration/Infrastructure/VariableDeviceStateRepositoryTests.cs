using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration test per VariableDeviceStateRepository.
/// Speculare a CommandDeviceStateRepositoryTests.
/// </summary>
public class VariableDeviceStateRepositoryTests : IntegrationTestBase
{
    private readonly VariableDeviceStateRepository _repository;
    private readonly DictionaryRepository _dictionaryRepo;
    private readonly VariableRepository _variableRepo;
    private VariableEntity _testVariable = null!;

    public VariableDeviceStateRepositoryTests()
    {
        _repository = new VariableDeviceStateRepository(Context);
        _dictionaryRepo = new DictionaryRepository(Context);
        _variableRepo = new VariableRepository(Context);
    }

    public override async Task InitializeAsync()
    {
        var dict = new DictionaryEntity { Name = "test-dict" };
        await _dictionaryRepo.AddAsync(dict);

        _testVariable = new VariableEntity
        {
            DictionaryId = dict.Id,
            Name = "TestVar",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "UInt16",
            AccessMode = AccessMode.ReadWrite,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(_testVariable);
    }

    [Fact]
    public async Task AddAsync_CreatesState()
    {
        var state = new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.OptimusXp,
            IsEnabled = false
        };

        var created = await _repository.AddAsync(state);

        Assert.True(created.Id > 0);
        Assert.Equal(_testVariable.Id, created.VariableId);
        Assert.Equal(DeviceType.OptimusXp, created.DeviceType);
        Assert.False(created.IsEnabled);
    }

    [Fact]
    public async Task GetByVariableAndDeviceAsync_Exists_ReturnsState()
    {
        await _repository.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.SherpaSlim,
            IsEnabled = false
        });

        var result = await _repository.GetByVariableAndDeviceAsync(
            _testVariable.Id, DeviceType.SherpaSlim);

        Assert.NotNull(result);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task GetByVariableAndDeviceAsync_NotExists_ReturnsNull()
    {
        var result = await _repository.GetByVariableAndDeviceAsync(
            _testVariable.Id, DeviceType.Spark);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByVariableIdAsync_ReturnsAllStates()
    {
        await _repository.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.SherpaSlim,
            IsEnabled = false
        });
        await _repository.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.Gradino,
            IsEnabled = false
        });

        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByVariableIdAsync_NoStates_ReturnsEmpty()
    {
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_ChangesIsEnabled()
    {
        var state = await _repository.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.EdenXp,
            IsEnabled = false
        });

        state.IsEnabled = true;
        await _repository.UpdateAsync(state);

        var updated = await _repository.GetByIdAsync(state.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsEnabled);
    }

    [Fact]
    public async Task DeleteAsync_RemovesState()
    {
        var state = await _repository.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.TopLiftA2,
            IsEnabled = false
        });

        await _repository.DeleteAsync(state.Id);

        var result = await _repository.GetByIdAsync(state.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateVariableDevice_ThrowsException()
    {
        await _repository.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.Spark,
            IsEnabled = false
        });

        var duplicate = new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.Spark,
            IsEnabled = true
        };

        await Assert.ThrowsAnyAsync<Exception>(
            () => _repository.AddAsync(duplicate));
    }

    [Fact]
    public async Task CascadeDelete_WhenVariableDeleted_StatesRemoved()
    {
        await _repository.AddAsync(new VariableDeviceStateEntity
        {
            VariableId = _testVariable.Id,
            DeviceType = DeviceType.SherpaSlim,
            IsEnabled = false
        });

        await _variableRepo.DeleteAsync(_testVariable.Id);

        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);
        Assert.Empty(result);
    }
}
