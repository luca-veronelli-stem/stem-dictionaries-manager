using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di un dizionario.
/// </summary>
public partial class DictionaryEditViewModel : ObservableObject
{
    private readonly IDictionaryService _dictionaryService;
    private readonly IBoardService _boardService;
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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private BoardTypeItem? _selectedBoardType;

    [ObservableProperty]
    private List<BoardTypeItem> _availableBoardTypes = [];

    /// <summary>
    /// True se stiamo creando un nuovo dizionario, false se modifica.
    /// </summary>
    public bool IsNew => _editingId is null;

    /// <summary>
    /// Titolo del form.
    /// </summary>
    public string FormTitle => IsNew ? "Nuovo Dizionario" : "Modifica Dizionario";

    /// <summary>
    /// BoardType modificabile solo per nuovi dizionari.
    /// </summary>
    public bool CanChangeBoardType => IsNew;

    public DictionaryEditViewModel(
        IDictionaryService dictionaryService,
        IBoardService boardService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _dictionaryService = dictionaryService;
        _boardService = boardService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    // Partial methods generati da [ObservableProperty] per tracciare le modifiche
    partial void OnNameChanged(string value) => HasChanges = true;
    partial void OnDescriptionChanged(string value) => HasChanges = true;
    partial void OnSelectedBoardTypeChanged(BoardTypeItem? value) => HasChanges = true;

    /// <summary>
    /// Inizializza il ViewModel con l'ID del dizionario da modificare.
    /// </summary>
    /// <param name="dictionaryId">ID del dizionario, null per nuovo.</param>
    public async Task InitializeAsync(int? dictionaryId)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _editingId = dictionaryId;

            // Carica i BoardType disponibili
            var boardTypes = await _boardService.GetBoardTypesAsync();
            AvailableBoardTypes = [.. boardTypes
                .Select(bt => new BoardTypeItem
                {
                    Id = bt.Id,
                    Name = bt.Name,
                    FirmwareType = bt.FirmwareType
                })];

            // Se modifica, carica i dati esistenti
            if (dictionaryId.HasValue)
            {
                var dictionary = await _dictionaryService.GetByIdAsync(dictionaryId.Value);
                if (dictionary is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Dizionario non trovato.");
                    _navigationService.GoBack();
                    return;
                }

                Name = dictionary.Name;
                OnPropertyChanged(nameof(Name));
                Description = dictionary.Description ?? string.Empty;
                OnPropertyChanged(nameof(Description));
                SelectedBoardType = AvailableBoardTypes
                    .FirstOrDefault(bt => bt.Id == dictionary.BoardType?.Id);
                OnPropertyChanged(nameof(SelectedBoardType));
            }

            _isInitialized = true;
            HasChanges = false;
            
            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(CanChangeBoardType));
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

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        try
        {
            IsBusy = true;

            if (IsNew)
            {
                // Trova il BoardType domain model se selezionato
                BoardType? boardType = null;
                if (SelectedBoardType is not null)
                {
                    var boardTypes = await _boardService.GetBoardTypesAsync();
                    boardType = boardTypes.FirstOrDefault(bt => bt.Id == SelectedBoardType.Id);
                }

                var dictionary = new Core.Models.Dictionary(
                    Name, 
                    boardType, 
                    string.IsNullOrWhiteSpace(Description) ? null : Description);

                await _dictionaryService.AddAsync(dictionary);
                _messageService.Show($"Dizionario '{Name}' creato", MessageSeverity.Success);
            }
            else
            {
                var existing = await _dictionaryService.GetByIdAsync(_editingId!.Value);
                if (existing is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Dizionario non trovato.");
                    return;
                }

                // Ricrea il Domain model con i nuovi valori
                var updated = Core.Models.Dictionary.Restore(
                    existing.Id,
                    Name,
                    existing.BoardType,  // BoardType non modificabile
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    existing.Variables);

                await _dictionaryService.UpdateAsync(updated);
                _messageService.Show($"Dizionario '{Name}' aggiornato", MessageSeverity.Success);
            }

            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore salvataggio", ex.Message);
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
                "Modifiche non salvate",
                "Ci sono modifiche non salvate. Vuoi uscire senza salvare?");

            if (result != Abstractions.DialogResult.Yes)
                return;
        }

        _navigationService.GoBack();
    }

    private bool Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Il nome è obbligatorio.");
        else if (Name.Length > 100)
            errors.Add("Il nome non può superare 100 caratteri.");

        if (Description.Length > 500)
            errors.Add("La descrizione non può superare 500 caratteri.");

        if (errors.Count > 0)
        {
            ErrorMessage = string.Join("\n", errors);
            return false;
        }

        ErrorMessage = null;
        return true;
    }
}

/// <summary>
/// Item per dropdown BoardType.
/// </summary>
public class BoardTypeItem
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public int FirmwareType { get; init; }

    public override string ToString() => $"{Name} (FW: {FirmwareType})";
}
