using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di un comando.
/// </summary>
public partial class CommandEditViewModel : ObservableObject
{
    private readonly ICommandService _commandService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Campi editabili ===

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private byte _codeHigh;

    [ObservableProperty]
    private byte _codeLow;

    [ObservableProperty]
    private bool _isResponse;

    [ObservableProperty]
    private string _parametersText = string.Empty;

    // === Computed Properties ===

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuovo Comando" : "Modifica Comando";
    public string FullCodeDisplay => $"0x{(CodeHigh << 8 | CodeLow):X4}";

    public CommandEditViewModel(
        ICommandService commandService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _commandService = commandService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    /// <summary>
    /// Inizializza il ViewModel.
    /// </summary>
    public async Task InitializeAsync(int? commandId)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _editingId = commandId;

            if (commandId.HasValue)
            {
                var command = await _commandService.GetByIdAsync(commandId.Value);
                if (command is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Comando non trovato.");
                    _navigationService.GoBack();
                    return;
                }

                LoadFromCommand(command);
            }

            _isInitialized = true;
            HasChanges = false;

            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(FormTitle));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile caricare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadFromCommand(Command c)
    {
        Name = c.Name;
        CodeHigh = c.CodeHigh;
        CodeLow = c.CodeLow;
        IsResponse = c.IsResponse;
        ParametersText = string.Join(Environment.NewLine, c.Parameters);

        OnPropertyChanged(nameof(CodeHigh));
        OnPropertyChanged(nameof(CodeLow));
        OnPropertyChanged(nameof(FullCodeDisplay));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            var parameters = ParametersText
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (IsNew)
            {
                var command = new Command(
                    name: Name,
                    codeHigh: CodeHigh,
                    codeLow: CodeLow,
                    isResponse: IsResponse,
                    parameters: parameters);

                await _commandService.AddAsync(command);
                _messageService.Show($"Comando '{Name}' creato", MessageSeverity.Success);
            }
            else
            {
                var existing = Command.Restore(
                    id: _editingId!.Value,
                    name: Name,
                    codeHigh: CodeHigh,
                    codeLow: CodeLow,
                    isResponse: IsResponse,
                    parameters: parameters);

                await _commandService.UpdateAsync(existing);
                _messageService.Show($"Comando '{Name}' aggiornato", MessageSeverity.Success);
            }

            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile salvare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Conferma",
                "Ci sono modifiche non salvate. Uscire comunque?");
            if (result != DialogResult.Yes) return;
        }

        _navigationService.GoBack();
    }
}
