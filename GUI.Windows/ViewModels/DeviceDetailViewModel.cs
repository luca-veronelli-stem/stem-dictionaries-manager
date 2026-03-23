using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista dizionari di un device.
/// </summary>
public partial class DictionaryItem : ObservableObject
{
    public int Id { get; }
    public string Name { get; }
    public string? BoardTypeName { get; }
    public int VariableCount { get; }

    public DictionaryItem(int id, string name, string? boardTypeName, int variableCount)
    {
        Id = id;
        Name = name;
        BoardTypeName = boardTypeName;
        VariableCount = variableCount;
    }
}

/// <summary>
/// ViewModel per il dettaglio di un tipo dispositivo.
/// Mostra i dizionari associati alle schede di quel device.
/// </summary>
public partial class DeviceDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDictionaryService _dictionaryService;
    private readonly IBoardService _boardService;

    [ObservableProperty]
    private DeviceType? _deviceType;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DictionaryItem> _dictionaries = [];

    [ObservableProperty]
    private DictionaryItem? _selectedDictionary;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public DeviceDetailViewModel(
        INavigationService navigationService,
        IDictionaryService dictionaryService,
        IBoardService boardService)
    {
        _navigationService = navigationService;
        _dictionaryService = dictionaryService;
        _boardService = boardService;
    }

    /// <summary>
    /// Carica i dizionari per il device specificato.
    /// Chiamato da MainViewModel.InitializeViewModelAsync.
    /// </summary>
    public async Task LoadAsync(DeviceType deviceType)
    {
        DeviceType = deviceType;
        DeviceName = GetDeviceName(deviceType);
        await LoadDictionariesAsync();
    }

    private static string GetDeviceName(DeviceType deviceType) => deviceType switch
    {
        Core.Enums.DeviceType.SherpaSlim => "Sherpa Slim",
        Core.Enums.DeviceType.TopLiftM => "TopLift-M",
        Core.Enums.DeviceType.EdenXp => "Eden-XP",
        Core.Enums.DeviceType.Gradino => "Gradino",
        Core.Enums.DeviceType.Spyke => "Spyke",
        Core.Enums.DeviceType.Spark => "Spark",
        Core.Enums.DeviceType.TopLiftA2 => "TopLift-A2",
        Core.Enums.DeviceType.O3zTech => "O3Z-Tech",
        Core.Enums.DeviceType.OptimusXp => "Optimus-XP",
        Core.Enums.DeviceType.R3lXp => "R3L-XP",
        Core.Enums.DeviceType.EdenBs8 => "Eden-BS8",
        _ => deviceType.ToString()
    };

    private async Task LoadDictionariesAsync()
    {
        if (DeviceType == null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Trova le board di questo device type
            var boards = await _boardService.GetByDeviceTypeAsync(DeviceType.Value);

            // Raccogli i BoardTypeId unici
            var boardTypeIds = boards.Select(b => b.BoardType?.Id).Where(id => id.HasValue).Distinct().ToList();

            // Trova i dizionari per questi BoardType + il dizionario Standard
            var allDicts = await _dictionaryService.GetAllAsync();
            var relevantDicts = allDicts.Where(d =>
                d.BoardType == null || // Standard
                (d.BoardType != null && boardTypeIds.Contains(d.BoardType.Id)) ||
                d.DeviceType == DeviceType.Value // O associato direttamente al DeviceType
            ).ToList();

            var items = new List<DictionaryItem>();
            foreach (var dict in relevantDicts)
            {
                var dictWithVars = await _dictionaryService.GetWithVariablesAsync(dict.Id);
                var varCount = dictWithVars?.Variables.Count ?? 0;
                items.Add(new DictionaryItem(dict.Id, dict.Name, dict.BoardType?.Name, varCount));
            }

            Dictionaries = new ObservableCollection<DictionaryItem>(items.OrderBy(d => d.Name));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenDictionary()
    {
        if (SelectedDictionary == null) return;

        _navigationService.NavigateTo(ViewType.VariableList, new NavigationParameter
        {
            ParentId = SelectedDictionary.Id
        });
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
