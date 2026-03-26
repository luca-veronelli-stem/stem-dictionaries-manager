namespace GUI.Windows.Abstractions;

/// <summary>
/// Identificatori dei tipi di view per la navigazione.
/// </summary>
public enum ViewType
{
    DeviceList,
    DeviceDetail,
    DeviceCommands,
    DictionaryList,
    DictionaryEdit,
    VariableEdit,
    CommandList,
    CommandEdit,
    BoardEdit,
    UserList,
    Settings
}

/// <summary>
/// Parametri di navigazione per passare dati tra view.
/// </summary>
public record NavigationParameter
{
    /// <summary>
    /// ID dell'entità da editare (null = nuova entità).
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// ID del parent (es. DictionaryId per VariableList).
    /// </summary>
    public int? ParentId { get; init; }

    /// <summary>
    /// Tipo dispositivo per filtro (es. DeviceDetail).
    /// </summary>
    public Core.Enums.DeviceType? DeviceType { get; init; }

    /// <summary>
    /// Parametri aggiuntivi.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Extra { get; init; }
}

/// <summary>
/// Servizio di navigazione tra view.
/// Gestisce la history e il passaggio di parametri.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// View correntemente visualizzata.
    /// </summary>
    ViewType CurrentView { get; }

    /// <summary>
    /// Parametro di navigazione corrente.
    /// </summary>
    NavigationParameter? CurrentParameter { get; }

    /// <summary>
    /// Indica se è possibile tornare indietro.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// ViewModel ripristinato dal GoBack (null se navigazione forward).
    /// </summary>
    object? CachedViewModel { get; }

    /// <summary>
    /// Registra il ViewModel corrente per il caching nella history.
    /// </summary>
    void SetCurrentViewModel(object? viewModel);

    /// <summary>
    /// Naviga verso una view specifica.
    /// </summary>
    void NavigateTo(ViewType viewType, NavigationParameter? parameter = null);

    /// <summary>
    /// Torna alla view precedente.
    /// </summary>
    /// <returns>True se la navigazione è avvenuta, false se non c'è history.</returns>
    bool GoBack();

    /// <summary>
    /// Resetta lo stato di navigazione alla view iniziale.
    /// Pulisce history e cached ViewModel.
    /// </summary>
    void Reset();

    /// <summary>
    /// Evento sollevato quando cambia la view corrente.
    /// </summary>
    event EventHandler<ViewType>? CurrentViewChanged;
}
