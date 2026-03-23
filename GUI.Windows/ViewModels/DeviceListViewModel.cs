using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using GUI.Windows.Abstractions;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista dispositivi.
/// </summary>
public partial class DeviceItem : ObservableObject
{
    public DeviceType DeviceType { get; }
    public string Name { get; }
    public string Description { get; }

    public DeviceItem(DeviceType deviceType, string name, string description)
    {
        DeviceType = deviceType;
        Name = name;
        Description = description;
    }
}

/// <summary>
/// ViewModel per la lista dei tipi di dispositivo.
/// </summary>
public partial class DeviceListViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<DeviceItem> _devices = [];

    [ObservableProperty]
    private DeviceItem? _selectedDevice;

    [ObservableProperty]
    private string _searchText = string.Empty;

    private List<DeviceItem> _allDevices = [];

    public DeviceListViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        LoadDevices();
    }

    private void LoadDevices()
    {
        // Crea lista da enum DeviceType con descrizioni italiane
        _allDevices =
        [
            new DeviceItem(DeviceType.SherpaSlim, "Sherpa Slim", "Sistema di caricamento assistito a controllo elettronico"),
            new DeviceItem(DeviceType.TopLiftM, "TopLift-M", "Sollevatori oleodinamici serie civile"),
            new DeviceItem(DeviceType.EdenXp, "Eden-XP", "Supporto barella ammortizzato idropneumatico"),
            new DeviceItem(DeviceType.Gradino, "Gradino", "Gradini automatici"),
            new DeviceItem(DeviceType.Spyke, "Spyke", "Barella con sistema di caricamento assistito e manutenzione predittiva"),
            new DeviceItem(DeviceType.Spark, "Spark", "Barella elettrica robotizzata"),
            new DeviceItem(DeviceType.TopLiftA2, "TopLift-A2", "Sollevatori oleodinamici serie militare"),
            new DeviceItem(DeviceType.O3zTech, "O3Z-Tech", "Sistema di sanificazione per veicoli"),
            new DeviceItem(DeviceType.OptimusXp, "Optimus-XP", "Supporto per barelle elettriche"),
            new DeviceItem(DeviceType.R3lXp, "R3L-XP", "Supporto barella elettromeccanico con sollevamento e inclinazione"),
            new DeviceItem(DeviceType.EdenBs8, "Eden-BS8", "Supporto barella ammortizzato con inclinazione regolabile")
        ];

        ApplyFilter();
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
            DeviceType = target.DeviceType
        });
    }
}
