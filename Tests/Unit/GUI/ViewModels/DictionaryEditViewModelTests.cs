#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DictionaryEditViewModel (Domain v2).
/// IsStandard flag, nessun DeviceType/BoardType.
/// </summary>
public class DictionaryEditViewModelTests
{
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DictionaryEditViewModel _viewModel;

    public DictionaryEditViewModelTests()
    {
        _dictionaryService = new MockDictionaryService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new DictionaryEditViewModel(
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        await _viewModel.InitializeAsync(null);

        Assert.True(_viewModel.IsNew);
        Assert.Equal("Nuovo Dizionario", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        var dict = new Dictionary("Existing", "Desc");
        _dictionaryService.SeedData(dict);

        await _viewModel.InitializeAsync(1);

        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Dizionario", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_LoadsExistingData()
    {
        var dict = Dictionary.Restore(1, "TestDict", "TestDesc", true, []);
        _dictionaryService.SeedData(dict);

        await _viewModel.InitializeAsync(1);

        Assert.Equal("TestDict", _viewModel.Name);
        Assert.Equal("TestDesc", _viewModel.Description);
        Assert.True(_viewModel.IsStandard);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
        await _viewModel.InitializeAsync(999);

        Assert.Contains(_dialogService.Calls, c => c.Type == "Error" && c.Message.Contains("non trovato"));
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        await _viewModel.InitializeAsync(null);
        _dictionaryService.MethodCalls.Clear();
        await _viewModel.InitializeAsync(null);

        Assert.Empty(_dictionaryService.MethodCalls);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessage()
    {
        _dictionaryService.ExceptionToThrow = new Exception("Service error");

        await _viewModel.InitializeAsync(1);

        Assert.Equal("Service error", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveCommand_CannotExecute_WhenNameEmpty()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = string.Empty;

        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_CanExecute_WhenNameSet()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Test";

        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _viewModel.Description = "NewDesc";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("AddAsync:"));
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_CallsUpdateAsync()
    {
        var dict = new Dictionary("Existing");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);
        _viewModel.Name = "Updated";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("UpdateAsync:"));
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_messageService.Messages, m =>
            m.Message.Contains("creato") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _dictionaryService.ExceptionToThrow = new Exception("Save failed");

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c => c.Type == "Error");
    }

    [Fact]
    public async Task OnNameChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        _viewModel.Name = "Changed";

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task OnDescriptionChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        _viewModel.Description = "Changed description";

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task OnIsStandardChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        _viewModel.IsStandard = true;

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveCommand_WithIsStandard_CreatesStandardDictionary()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Standard";
        _viewModel.IsStandard = true;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("AddAsync:Standard", _dictionaryService.MethodCalls);
    }
}
#endif
