namespace GUI.Windows.Abstractions;

/// <summary>
/// Identificatori dei tipi di view per la navigazione.
/// </summary>
public enum ViewType
{
    DictionaryList,
    DictionaryEdit,
    VariableList,
    VariableEdit,
    CommandList,
    CommandEdit,
    BoardList,
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
    /// Naviga verso una view specifica.
    /// </summary>
    void NavigateTo(ViewType viewType, NavigationParameter? parameter = null);

    /// <summary>
    /// Torna alla view precedente.
    /// </summary>
    /// <returns>True se la navigazione è avvenuta, false se non c'è history.</returns>
    bool GoBack();

    /// <summary>
    /// Evento sollevato quando cambia la view corrente.
    /// </summary>
    event EventHandler<ViewType>? CurrentViewChanged;
}
