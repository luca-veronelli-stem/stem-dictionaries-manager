using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel for the dictionary list.
/// </summary>
public partial class DictionaryListViewModel : ObservableObject
{
    private readonly IDictionaryService _dictionaryService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    private List<DictionaryListItem> _allDictionaries = [];

    [ObservableProperty]
    private List<DictionaryListItem> _dictionaries = [];

    [ObservableProperty]
    private DictionaryListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Filters the list whenever SearchText changes.
    /// </summary>
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public DictionaryListViewModel(
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

    /// <summary>
    /// Loads the initial data. Call after navigating to the view.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            IReadOnlyList<Dictionary> dictionaries = await _dictionaryService.GetAllAsync();

            _allDictionaries = [.. dictionaries
                .Select(d => new DictionaryListItem
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    IsStandard = d.IsStandard,
                    VariableCount = d.Variables.Count
                })];

            ApplyFilter();
            _messageService.Show($"Loaded {_allDictionaries.Count} dictionaries", MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _messageService.Show($"Error: {ex.Message}", MessageSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Add()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter { EntityId = null });
    }

    [RelayCommand]
    private void Edit(DictionaryListItem? item)
    {
        if (item is null)
        {
            return;
        }

        _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter { EntityId = item.Id });
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Dictionaries = _allDictionaries;
            return;
        }

        string term = SearchText.Trim();
        Dictionaries = [.. _allDictionaries.Where(d =>
            d.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (d.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
            d.SemanticDisplay.Contains(term, StringComparison.OrdinalIgnoreCase))];
    }
}

/// <summary>
/// Display item for the dictionary list.
/// </summary>
public class DictionaryListItem
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsStandard { get; init; }
    public int VariableCount { get; init; }

    /// <summary>
    /// Semantic kind for the column.
    /// </summary>
    public string SemanticDisplay => IsStandard ? "Standard" : "Specific";
}
