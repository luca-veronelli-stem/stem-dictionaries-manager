using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di un dizionario.
/// Domain v2: IsStandard flag, nessun DeviceType/BoardType.
/// </summary>
public partial class DictionaryEditViewModel : ObservableObject
{
    private readonly IDictionaryService _dictionaryService;
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
    private bool _isStandard;

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuovo Dizionario" : "Modifica Dizionario";

    public DictionaryEditViewModel(
        IDictionaryService dictionaryService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _dictionaryService = dictionaryService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    partial void OnNameChanged(string value) => HasChanges = true;
    partial void OnDescriptionChanged(string value) => HasChanges = true;
    partial void OnIsStandardChanged(bool value) => HasChanges = true;

    public async Task InitializeAsync(int? dictionaryId)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _editingId = dictionaryId;

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
                Description = dictionary.Description ?? string.Empty;
                IsStandard = dictionary.IsStandard;
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
                var dictionary = new Core.Models.Dictionary(
                    Name,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    IsStandard);

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

                var updated = Core.Models.Dictionary.Restore(
                    existing.Id,
                    Name,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    IsStandard,
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
