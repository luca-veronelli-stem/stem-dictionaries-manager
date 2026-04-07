using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista dispositivi.
/// </summary>
public partial class DeviceItem : ObservableObject
{
    public int DeviceId { get; }
    public string Name { get; }
    public string Description { get; }
    public int MachineCode { get; }
    public int BoardCount { get; }
    public int DictionaryCount { get; }

    public DeviceItem(int deviceId, string name, string description, int machineCode,
        int boardCount = 0, int dictionaryCount = 0)
    {
        DeviceId = deviceId;
        Name = name;
        Description = description;
        MachineCode = machineCode;
        BoardCount = boardCount;
        DictionaryCount = dictionaryCount;
    }
}

/// <summary>
/// ViewModel per la lista dei dispositivi.
/// SESSION_035: carica da DB tramite IDeviceService.
/// </summary>
public partial class DeviceListViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDeviceService _deviceService;
    private readonly IBoardService _boardService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private ObservableCollection<DeviceItem> _devices = [];

    [ObservableProperty]
    private DeviceItem? _selectedDevice;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    private List<DeviceItem> _allDevices = [];

    public DeviceListViewModel(
        INavigationService navigationService,
        IDeviceService deviceService,
        IBoardService boardService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _navigationService = navigationService;
        _deviceService = deviceService;
        _boardService = boardService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var devices = await _deviceService.GetAllAsync();

            // Carica tutte le board per derivare conteggi per device
            var boardsByDevice = new Dictionary<int, List<Core.Models.Board>>();
            foreach (var device in devices)
            {
                var boards = await _boardService.GetByDeviceIdAsync(device.Id);
                boardsByDevice[device.Id] = boards.ToList();
            }

            _allDevices = [.. devices
                .OrderBy(d => d.MachineCode)
                .Select(d =>
                {
                    var boards = boardsByDevice.GetValueOrDefault(d.Id, []);
                    var boardCount = boards.Count;
                    var dictionaryCount = boards
                        .Where(b => b.DictionaryId.HasValue)
                        .Select(b => b.DictionaryId!.Value)
                        .Distinct()
                        .Count();
                    return new DeviceItem(
                        d.Id, d.Name, d.Description ?? string.Empty, d.MachineCode,
                        boardCount, dictionaryCount);
                })];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore caricamento dispositivi: {ex.Message}",
                MessageSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Devices = new ObservableCollection<DeviceItem>(_allDevices);
        }
        else
        {
            var filtered = _allDevices.Where(d =>
                d.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                d.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            Devices = new ObservableCollection<DeviceItem>(filtered);
        }
    }

    [RelayCommand]
    private void SelectDevice(DeviceItem? device)
    {
        SelectedDevice = device;
    }

    [RelayCommand]
    private void OpenDevice(DeviceItem? device)
    {
        var target = device ?? SelectedDevice;
        if (target == null) return;

        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceId = target.DeviceId
        });
    }

    [RelayCommand]
    private void AddDevice()
    {
        _navigationService.NavigateTo(ViewType.DeviceEdit, new NavigationParameter
        {
            EntityId = null
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteDeviceAsync()
    {
        await _dialogService.ShowErrorAsync(
            "Operazione non consentita",
            "Solo l'amministratore può eliminare un dispositivo.");
    }
}
