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
    public void CurrentView_DefaultsToDictionaryList()
    {
        // Assert
        Assert.Equal(ViewType.DictionaryList, _service.CurrentView);
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
        _service.NavigateTo(ViewType.VariableList);

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
        _service.NavigateTo(ViewType.VariableList, param2);

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
        Assert.Equal(ViewType.DictionaryList, _service.CurrentView);
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
        Assert.Equal(ViewType.DictionaryList, eventView);
    }

    [Fact]
    public void GoBack_AfterMultipleNavigations_RestoresCorrectly()
    {
        // Arrange
        _service.NavigateTo(ViewType.DictionaryEdit);
        _service.NavigateTo(ViewType.VariableList);
        _service.NavigateTo(ViewType.CommandList);

        // Act & Assert
        _service.GoBack();
        Assert.Equal(ViewType.VariableList, _service.CurrentView);
        
        _service.GoBack();
        Assert.Equal(ViewType.DictionaryEdit, _service.CurrentView);
        
        _service.GoBack();
        Assert.Equal(ViewType.DictionaryList, _service.CurrentView);
        
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
}
#endif
