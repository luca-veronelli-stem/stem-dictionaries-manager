using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di un comando.
/// </summary>
public partial class CommandEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly ICommandService _commandService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private bool _isInitialized;
    private bool _showValidation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Campi editabili ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private string _name = string.Empty;

    /// <summary>
    /// CodeHigh calcolato automaticamente da IsResponse.
    /// 0x80 = risposta, 0x00 = comando.
    /// </summary>
    public string CodeHighHex => IsResponse ? "80" : "00";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullCodeDisplay))]
    [NotifyPropertyChangedFor(nameof(IsCodeLowInvalid))]
    private string _codeLowHex = string.Empty;

    private byte CodeHigh => byte.TryParse(CodeHighHex, System.Globalization.NumberStyles.HexNumber, null, out byte v) ? v : (byte)0;
    private byte CodeLow => byte.TryParse(CodeLowHex, System.Globalization.NumberStyles.HexNumber, null, out byte v) ? v : (byte)0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CodeHighHex))]
    [NotifyPropertyChangedFor(nameof(FullCodeDisplay))]
    private bool _isResponse;

    /// <summary>
    /// Parametri strutturati per la DataGrid.
    /// </summary>
    public ObservableCollection<CommandParameterItem> ParameterItems { get; } = [];

    /// <summary>
    /// True se ci sono parametri da visualizzare.
    /// </summary>
    public bool HasParameters => ParameterItems.Count > 0;

    // === Computed Properties ===

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuovo Comando" : "Modifica Comando";
    public string FullCodeDisplay => $"0x{(CodeHigh << 8 | CodeLow):X4}";

    // === Proprietà di validazione per-campo (visibili solo dopo primo tentativo di salvataggio) ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);
    public bool IsCodeLowInvalid => _showValidation && string.IsNullOrWhiteSpace(CodeLowHex);

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
        if (_isInitialized)
        {
            return;
        }

        try
        {
            IsBusy = true;
            _editingId = commandId;

            if (commandId.HasValue)
            {
                Command? command = await _commandService.GetByIdAsync(commandId.Value);
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
        // CodeHighHex è computed automaticamente da IsResponse
        CodeLowHex = c.CodeLow.ToString("X2");
        IsResponse = c.IsResponse;

        // Carica parametri strutturati
        ParameterItems.Clear();
        foreach ((string? p, int i) in c.Parameters.Select((p, i) => (p, i)))
        {
            var item = CommandParameterItem.Deserialize(i, p);
            item.PropertyChanged += (_, _) => HasChanges = true;
            ParameterItems.Add(item);
        }

        OnPropertyChanged(nameof(HasParameters));
        OnPropertyChanged(nameof(FullCodeDisplay));
    }

    private bool Validate()
    {
        _showValidation = true;

        OnPropertyChanged(nameof(IsNameInvalid));
        OnPropertyChanged(nameof(IsCodeLowInvalid));

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            missing.Add("Nome");
        }

        if (string.IsNullOrWhiteSpace(CodeLowHex))
        {
            missing.Add("Codice");
        }

        if (missing.Count > 0)
        {
            _messageService.Show($"Campi obbligatori mancanti: {string.Join(", ", missing)}", MessageSeverity.Warning);
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            return;
        }

        try
        {
            IsBusy = true;

            var parameters = ParameterItems
                .Select(p => p.Serialize())
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
    private async Task DeleteAsync()
    {
        if (IsNew)
        {
            return;
        }

        DialogResult result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"Eliminare il comando '{Name}'?\nQuesta operazione non può essere annullata.");

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _commandService.DeleteAsync(_editingId!.Value);
            _messageService.Show($"Comando '{Name}' eliminato", MessageSeverity.Success);
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile eliminare: {ex.Message}");
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
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Annulla modifiche",
                "Sei sicuro di voler annullare le modifiche?");
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }

    /// <summary>
    /// Aggiunge un nuovo parametro alla lista.
    /// </summary>
    [RelayCommand]
    private void AddParameter()
    {
        var item = new CommandParameterItem { Index = ParameterItems.Count };
        item.PropertyChanged += (_, _) => HasChanges = true;
        ParameterItems.Add(item);
        OnPropertyChanged(nameof(HasParameters));
        HasChanges = true;
    }

    /// <summary>
    /// Rimuove l'ultimo parametro dalla lista.
    /// </summary>
    [RelayCommand]
    private void RemoveLastParameter()
    {
        if (ParameterItems.Count == 0)
        {
            return;
        }

        ParameterItems.RemoveAt(ParameterItems.Count - 1);
        OnPropertyChanged(nameof(HasParameters));
        HasChanges = true;
    }
}
