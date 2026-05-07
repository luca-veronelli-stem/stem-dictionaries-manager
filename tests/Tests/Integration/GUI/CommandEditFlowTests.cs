#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso completo di gestione comandi.
/// Pattern identico a VariableEditFlowTests.
/// </summary>
public class CommandEditFlowTests
{
    private readonly MockCommandService _commandService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly CommandEditViewModel _viewModel;

    public CommandEditFlowTests()
    {
        _commandService = new MockCommandService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new CommandEditViewModel(
            _commandService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task CreateCommand_WithValidData_SavesAndNavigatesBack()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "ReadStatus";
        _viewModel.CodeLowHex = "10";
        _viewModel.IsResponse = false;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_commandService.MethodCalls, m => m.StartsWith("AddAsync:ReadStatus"));
        Assert.True(_navigationService.GoBackCalled);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task CreateCommand_WithStructuredParameters_SerializesCorrectly()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "WriteRegister";
        _viewModel.CodeLowHex = "20";
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.ParameterItems[0].SizeBytes = "2";
        _viewModel.ParameterItems[0].Description = "Indirizzo memoria";
        _viewModel.ParameterItems[1].SizeBytes = "4";
        _viewModel.ParameterItems[1].Description = "Valore registro";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var saved = _commandService.GetSavedCommand();
        Assert.NotNull(saved);
        Assert.Equal("WriteRegister", saved.Name);
        Assert.Equal(2, saved.Parameters.Count);
        Assert.Equal("2|Indirizzo memoria", saved.Parameters[0]);
        Assert.Equal("4|Valore registro", saved.Parameters[1]);
    }

    [Fact]
    public async Task EditExistingCommand_LoadsAndUpdates()
    {
        var command = new Command("OldName", 0x00, 0x15, false, ["1|Param1"]);
        _commandService.SeedData(command);

        await _viewModel.InitializeAsync(1);
        Assert.Equal("OldName", _viewModel.Name);
        Assert.Equal("15", _viewModel.CodeLowHex);
        Assert.Single(_viewModel.ParameterItems);

        _viewModel.Name = "NewName";
        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("UpdateAsync:1", _commandService.MethodCalls);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task EditCommand_WithLegacyParameters_LoadsCorrectly()
    {
        var command = new Command("LegacyCmd", 0x00, 0x30, false, ["param1", "param2", "param3"]);
        _commandService.SeedData(command);

        await _viewModel.InitializeAsync(1);

        Assert.Equal(3, _viewModel.ParameterItems.Count);
        Assert.All(_viewModel.ParameterItems, item => Assert.Equal("", item.SizeBytes));
        Assert.Equal("param1", _viewModel.ParameterItems[0].Description);
        Assert.Equal("param2", _viewModel.ParameterItems[1].Description);
        Assert.Equal("param3", _viewModel.ParameterItems[2].Description);
    }

    [Fact]
    public async Task ValidationFlow_FailThenFixThenSucceed()
    {
        await _viewModel.InitializeAsync(null);

        // Primo tentativo: tutti i campi vuoti → validazione fallisce
        await _viewModel.SaveCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsNameInvalid);
        Assert.True(_viewModel.IsCodeLowInvalid);
        Assert.DoesNotContain(_commandService.MethodCalls, m => m.StartsWith("AddAsync"));

        // Fix campi
        _viewModel.Name = "FixedCommand";
        _viewModel.CodeLowHex = "01";
        Assert.False(_viewModel.IsNameInvalid);
        Assert.False(_viewModel.IsCodeLowInvalid);

        // Secondo tentativo: successo
        await _viewModel.SaveCommand.ExecuteAsync(null);
        Assert.Contains(_commandService.MethodCalls, m => m.StartsWith("AddAsync:FixedCommand"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteCommand_ConfirmFlow_DeletesAndNavigatesBack()
    {
        var command = new Command("ToDelete", 0x00, 0x40, false);
        _commandService.SeedData(command);
        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Contains("DeleteAsync:1", _commandService.MethodCalls);
        Assert.True(_navigationService.GoBackCalled);
        Assert.Contains(_messageService.Messages, m =>
            m.Severity == MessageSeverity.Success && m.Message.Contains("eliminato"));
    }

    [Fact]
    public async Task CancelWithUnsavedChanges_ConfirmFlow_NavigatesBack()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "UnsavedCommand";
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c =>
            c.Type == "Confirm" && c.Message.Contains("annullare"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CreateResponse_CodeHighIs80InSavedCommand()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "StatusResponse";
        _viewModel.CodeLowHex = "10";
        _viewModel.IsResponse = true;
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.ParameterItems[0].SizeBytes = "2";
        _viewModel.ParameterItems[0].Description = "Status code";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var saved = _commandService.GetSavedCommand();
        Assert.NotNull(saved);
        Assert.True(saved.IsResponse);
        Assert.Equal(0x80, saved.CodeHigh);
        Assert.Equal(0x10, saved.CodeLow);
        Assert.Single(saved.Parameters);
        Assert.Equal("2|Status code", saved.Parameters[0]);
    }
}
#endif
