#if WINDOWS
using GUI.Windows.Abstractions;
using GUI.Windows.Services;

namespace Tests.Unit.GUI.Services;

/// <summary>
/// Test per NavigationService.
/// </summary>
public class NavigationServiceTests
{
    private readonly NavigationService _service;

    public NavigationServiceTests()
    {
        _service = new NavigationService();
    }

    [Fact]
    public void CurrentView_DefaultsToDeviceList()
    {
        // Assert
        Assert.Equal(ViewType.DeviceList, _service.CurrentView);
    }

    [Fact]
    public void CanGoBack_DefaultsFalse()
    {
        // Assert
        Assert.False(_service.CanGoBack);
    }

    [Fact]
    public void NavigateTo_ChangesCurrentView()
    {
        // Act
        _service.NavigateTo(ViewType.DictionaryEdit);

        // Assert
        Assert.Equal(ViewType.DictionaryEdit, _service.CurrentView);
    }

    [Fact]
    public void NavigateTo_SetsParameter()
    {
        // Arrange
        var param = new NavigationParameter { EntityId = 5 };

        // Act
        _service.NavigateTo(ViewType.DictionaryEdit, param);

        // Assert
        Assert.Equal(5, _service.CurrentParameter?.EntityId);
    }

    [Fact]
    public void NavigateTo_SetsCanGoBackTrue()
    {
        // Act
        _service.NavigateTo(ViewType.DictionaryEdit);

        // Assert
        Assert.True(_service.CanGoBack);
    }

    [Fact]
    public void NavigateTo_RaisesCurrentViewChanged()
    {
        // Arrange
        ViewType? eventView = null;
        _service.CurrentViewChanged += (s, v) => eventView = v;

        // Act
        _service.NavigateTo(ViewType.DictionaryEdit);

        // Assert
        Assert.Equal(ViewType.DictionaryEdit, eventView);
    }

    [Fact]
    public void GoBack_ReturnsToPreviousView()
    {
        // Arrange
        _service.NavigateTo(ViewType.DictionaryEdit);
        _service.NavigateTo(ViewType.VariableEdit);

        // Act
        var result = _service.GoBack();

        // Assert
        Assert.True(result);
        Assert.Equal(ViewType.DictionaryEdit, _service.CurrentView);
    }

    [Fact]
    public void GoBack_RestoresPreviousParameter()
    {
        // Arrange
        var param1 = new NavigationParameter { EntityId = 1 };
        var param2 = new NavigationParameter { EntityId = 2 };
        _service.NavigateTo(ViewType.DictionaryEdit, param1);
        _service.NavigateTo(ViewType.VariableEdit, param2);

        // Act
        _service.GoBack();

        // Assert
        Assert.Equal(1, _service.CurrentParameter?.EntityId);
    }

    [Fact]
    public void GoBack_WhenNoHistory_ReturnsFalse()
    {
        // Act
        var result = _service.GoBack();

        // Assert
        Assert.False(result);
        Assert.Equal(ViewType.DeviceList, _service.CurrentView);
    }

    [Fact]
    public void GoBack_RaisesCurrentViewChanged()
    {
        // Arrange
        _service.NavigateTo(ViewType.DictionaryEdit);
        ViewType? eventView = null;
        _service.CurrentViewChanged += (s, v) => eventView = v;

        // Act
        _service.GoBack();

        // Assert
        Assert.Equal(ViewType.DeviceList, eventView);
    }

    [Fact]
    public void GoBack_AfterMultipleNavigations_RestoresCorrectly()
    {
        // Arrange
        _service.NavigateTo(ViewType.DictionaryEdit);
        _service.NavigateTo(ViewType.VariableEdit);
        _service.NavigateTo(ViewType.CommandList);

        // Act & Assert
        _service.GoBack();
        Assert.Equal(ViewType.VariableEdit, _service.CurrentView);

        _service.GoBack();
        Assert.Equal(ViewType.DictionaryEdit, _service.CurrentView);

        _service.GoBack();
        Assert.Equal(ViewType.DeviceList, _service.CurrentView);

        Assert.False(_service.CanGoBack);
    }

    [Fact]
    public void NavigationParameter_Extra_WorksCorrectly()
    {
        // Arrange
        var extra = new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 };
        var param = new NavigationParameter
        {
            EntityId = 1,
            ParentId = 2,
            Extra = extra
        };

        // Act
        _service.NavigateTo(ViewType.DictionaryEdit, param);

        // Assert
        Assert.Equal(1, _service.CurrentParameter?.EntityId);
        Assert.Equal(2, _service.CurrentParameter?.ParentId);
        Assert.Equal("value1", _service.CurrentParameter?.Extra?["key1"]);
        Assert.Equal(42, _service.CurrentParameter?.Extra?["key2"]);
    }

    [Fact]
    public void NavigationParameter_DeviceType_WorksCorrectly()
    {
        // Arrange
        var param = new NavigationParameter
        {
            DeviceType = Core.Enums.DeviceType.OptimusXp
        };

        // Act
        _service.NavigateTo(ViewType.DeviceDetail, param);

        // Assert
        Assert.Equal(ViewType.DeviceDetail, _service.CurrentView);
        Assert.Equal(Core.Enums.DeviceType.OptimusXp, _service.CurrentParameter?.DeviceType);
    }

    [Fact]
    public void NavigateTo_DeviceList_ChangesView()
    {
        // Act
        _service.NavigateTo(ViewType.DeviceList);

        // Assert
        Assert.Equal(ViewType.DeviceList, _service.CurrentView);
    }

    [Fact]
    public void GoBack_FromDeviceDetail_RestoresDeviceList()
    {
        // Arrange
        _service.NavigateTo(ViewType.DeviceList);
        _service.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = Core.Enums.DeviceType.SherpaSlim
        });

        // Act
        _service.GoBack();

        // Assert
        Assert.Equal(ViewType.DeviceList, _service.CurrentView);
        Assert.Null(_service.CurrentParameter);
    }

    [Fact]
    public void SetCurrentViewModel_GoBack_RestoresCachedViewModel()
    {
        // Arrange
        var fakeVm = new object();
        _service.SetCurrentViewModel(fakeVm);
        _service.NavigateTo(ViewType.VariableEdit);

        // Act
        _service.GoBack();

        // Assert
        Assert.Same(fakeVm, _service.CachedViewModel);
    }

    [Fact]
    public void NavigateTo_ClearsCachedViewModel()
    {
        // Arrange - simulate a GoBack that sets CachedViewModel
        var fakeVm = new object();
        _service.SetCurrentViewModel(fakeVm);
        _service.NavigateTo(ViewType.DictionaryEdit);
        _service.GoBack(); // CachedViewModel = fakeVm

        // Act - forward navigation should clear it
        _service.NavigateTo(ViewType.CommandList);

        // Assert
        Assert.Null(_service.CachedViewModel);
    }

    [Fact]
    public void CachedViewModel_IsNull_OnForwardNavigation()
    {
        // Act
        _service.NavigateTo(ViewType.DictionaryEdit);

        // Assert
        Assert.Null(_service.CachedViewModel);
    }

    [Fact]
    public void CachedViewModel_IsNull_WhenNoViewModelSet()
    {
        // Arrange - navigate without SetCurrentViewModel
        _service.NavigateTo(ViewType.DictionaryEdit);

        // Act
        _service.GoBack();

        // Assert - no ViewModel was registered, so null
        Assert.Null(_service.CachedViewModel);
    }

    [Fact]
    public void SetCurrentViewModel_MultipleNavigations_PreservesEachLevel()
    {
        // Arrange
        var vm1 = new object();
        var vm2 = new object();

        _service.SetCurrentViewModel(vm1);      // DictionaryList has vm1
        _service.NavigateTo(ViewType.DictionaryEdit);
        _service.SetCurrentViewModel(vm2);      // DictionaryEdit has vm2
        _service.NavigateTo(ViewType.VariableEdit);

        // Act & Assert - going back should restore in order
        _service.GoBack();
        Assert.Same(vm2, _service.CachedViewModel);

        _service.GoBack();
        Assert.Same(vm1, _service.CachedViewModel);
    }

    [Fact]
    public void Reset_ResetsToDeviceList()
    {
        // Arrange
        _service.NavigateTo(ViewType.CommandList);

        // Act
        _service.Reset();

        // Assert
        Assert.Equal(ViewType.DeviceList, _service.CurrentView);
    }

    [Fact]
    public void Reset_ClearsHistory()
    {
        // Arrange
        _service.NavigateTo(ViewType.DictionaryEdit);
        _service.NavigateTo(ViewType.VariableEdit);

        // Act
        _service.Reset();

        // Assert
        Assert.False(_service.CanGoBack);
    }

    [Fact]
    public void Reset_ClearsParameterAndCachedViewModel()
    {
        // Arrange
        var fakeVm = new object();
        _service.SetCurrentViewModel(fakeVm);
        _service.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter { EntityId = 5 });
        _service.GoBack(); // CachedViewModel = fakeVm

        // Act
        _service.Reset();

        // Assert
        Assert.Null(_service.CurrentParameter);
        Assert.Null(_service.CachedViewModel);
    }
}
#endif
